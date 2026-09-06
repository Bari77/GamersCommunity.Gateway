using System.Text;
using System.Text.Json;
using GamersCommunity.Core.Rabbit;
using Gateway.Hubs;
using Gateway.Serialization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Gateway.Realtime;

public sealed class RealtimeEventsWorker(
    IOptions<RabbitMQSettings> opts,
    IHubContext<MessengerHub> hubContext,
    ILogger logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new UtcDateTimeJsonConverter() },
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = opts.Value.Hostname,
            UserName = opts.Value.Username,
            Password = opts.Value.Password,
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var connection = await factory.CreateConnectionAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: RealtimeQueues.Gateway,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) =>
                {
                    try
                    {
                        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                        await DispatchAsync(json, stoppingToken);
                        await channel.BasicAckAsync(ea.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to process realtime event.");
                        await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                };

                await channel.BasicConsumeAsync(
                    queue: RealtimeQueues.Gateway,
                    autoAck: false,
                    consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.Information("Realtime events worker listening on '{Queue}'.", RealtimeQueues.Gateway);

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Realtime events worker disconnected; retrying in 3s.");
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private async Task DispatchAsync(string json, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(json);
        var type = doc.RootElement.TryGetProperty("type", out var typeProp)
            ? typeProp.GetString()
            : null;

        if (string.Equals(type, RealtimeEventTypes.MessageCreated, StringComparison.OrdinalIgnoreCase))
        {
            await DispatchMessageCreatedAsync(json, ct);
            return;
        }

        if (string.Equals(type, RealtimeEventTypes.FriendUpdated, StringComparison.OrdinalIgnoreCase))
        {
            await DispatchFriendUpdatedAsync(json, ct);
            return;
        }

        if (string.Equals(type, RealtimeEventTypes.NotificationCreated, StringComparison.OrdinalIgnoreCase))
        {
            await DispatchNotificationCreatedAsync(json, ct);
            return;
        }

        logger.Debug("Ignoring unknown realtime event type '{Type}'.", type);
    }

    private async Task DispatchMessageCreatedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<MessageCreatedRealtimeEvent>(json, JsonOpts);
        if (evt?.Message is null
            || string.IsNullOrWhiteSpace(evt.SenderKeycloak)
            || string.IsNullOrWhiteSpace(evt.ReceiverKeycloak))
        {
            logger.Warning("Invalid message.created realtime payload.");
            return;
        }

        var payload = new
        {
            id = evt.Message.Id,
            publicId = evt.Message.PublicId,
            content = evt.Message.Content,
            idSender = evt.Message.IdSender,
            idReceiver = evt.Message.IdReceiver,
            isRead = evt.Message.IsRead,
            creationDate = UtcDateTimeJsonConverter.AsUtc(evt.Message.CreationDate),
            parentMessageId = evt.Message.ParentMessageId,
            parentContent = evt.Message.ParentContent,
        };

        await hubContext.Clients
            .Groups([RealtimeGroups.User(evt.SenderKeycloak), RealtimeGroups.User(evt.ReceiverKeycloak)])
            .SendAsync(RealtimeHubMethods.MessageCreated, payload, ct);
    }

    private async Task DispatchFriendUpdatedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<FriendUpdatedRealtimeEvent>(json, JsonOpts);
        if (evt is null
            || string.IsNullOrWhiteSpace(evt.AskingKeycloak)
            || string.IsNullOrWhiteSpace(evt.ReceivingKeycloak)
            || evt.PublicId == Guid.Empty)
        {
            logger.Warning("Invalid friend.updated realtime payload.");
            return;
        }

        var payload = new
        {
            publicId = evt.PublicId,
            idFriendAsking = evt.IdFriendAsking,
            idFriendReceive = evt.IdFriendReceive,
            idFriendStatus = evt.IdFriendStatus,
        };

        await hubContext.Clients
            .Groups([RealtimeGroups.User(evt.AskingKeycloak), RealtimeGroups.User(evt.ReceivingKeycloak)])
            .SendAsync(RealtimeHubMethods.FriendUpdated, payload, ct);
    }

    private async Task DispatchNotificationCreatedAsync(string json, CancellationToken ct)
    {
        var evt = JsonSerializer.Deserialize<NotificationCreatedRealtimeEvent>(json, JsonOpts);
        if (evt?.Notification is null || string.IsNullOrWhiteSpace(evt.RecipientKeycloak))
        {
            logger.Warning("Invalid notification.created realtime payload.");
            return;
        }

        var n = evt.Notification;
        var payload = new
        {
            id = n.Id,
            publicId = n.PublicId,
            idUser = n.IdUser,
            kind = n.Kind,
            title = n.Title,
            body = n.Body,
            linkUrl = n.LinkUrl,
            isRead = n.IsRead,
            payloadJson = n.PayloadJson,
            creationDate = UtcDateTimeJsonConverter.AsUtc(n.CreationDate),
        };

        await hubContext.Clients
            .Group(RealtimeGroups.User(evt.RecipientKeycloak))
            .SendAsync(RealtimeHubMethods.NotificationCreated, payload, ct);
    }
}
