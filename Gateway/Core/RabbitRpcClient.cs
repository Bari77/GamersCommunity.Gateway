using System.Text;
using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Gateway.Core;

public sealed class RabbitRpcClient : IRabbitRpcClient, IAsyncDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger _logger;
    private readonly ConnectionFactory _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _connection;

    public RabbitRpcClient(IOptions<RabbitMQSettings> opts, ILogger logger)
    {
        _settings = opts.Value;
        _logger = logger;
        _factory = new ConnectionFactory
        {
            HostName = _settings.Hostname,
            UserName = _settings.Username,
            Password = _settings.Password,
        };
    }

    public async Task<string> CallAsync(string queue, string payload, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(queue))
            throw new BadRequestException("QUEUE_NULL", "Queue name must not be null or empty.");
        if (string.IsNullOrWhiteSpace(payload))
            throw new BadRequestException("MESSAGE_NULL", "Message must not be null or empty.");

        var connection = await EnsureConnectionAsync(ct);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        var replyQueue = await channel.QueueDeclareAsync(
            queue: string.Empty,
            durable: false,
            exclusive: true,
            autoDelete: true,
            arguments: null,
            cancellationToken: ct);

        var correlationId = Guid.NewGuid().ToString("N");
        var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        string consumerTag = string.Empty;

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                if (ea.BasicProperties?.CorrelationId != correlationId)
                {
                    return;
                }

                var responseJson = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.Debug("RPC response received (corrId={CorrelationId}).", correlationId);

                try
                {
                    var envelope = JsonConvert.DeserializeObject<RpcEnvelope<string?>>(responseJson);
                    if (envelope is null)
                        throw new RpcException("INVALID_RESPONSE", "Response cannot be deserialized.", responseJson);

                    if (!envelope.Ok)
                        throw new RpcException(
                            envelope.Error?.Code ?? "ERROR",
                            envelope.Error?.Message ?? "Unknown error");

                    tcs.TrySetResult(envelope.Data ?? string.Empty);
                }
                catch (JsonException jex)
                {
                    _logger.Warning(jex, "Response is not a valid envelope. Returning raw body.");
                    tcs.TrySetResult(responseJson);
                }
                catch (RpcException rex)
                {
                    tcs.TrySetException(rex);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error while handling RPC response (corrId={CorrelationId}).", correlationId);
                tcs.TrySetException(ex);
            }
            finally
            {
                try
                {
                    await channel.BasicAckAsync(ea.DeliveryTag, multiple: false, cancellationToken: ct);
                }
                catch (Exception ackEx)
                {
                    _logger.Warning(ackEx, "Failed to ACK RPC response (corrId={CorrelationId}).", correlationId);
                }
            }
        };

        try
        {
            consumerTag = await channel.BasicConsumeAsync(
                queue: replyQueue.QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: ct);

            var props = new BasicProperties
            {
                CorrelationId = correlationId,
                ReplyTo = replyQueue.QueueName,
                ContentType = "application/json",
                ContentEncoding = "utf-8",
            };

            _logger.Debug("Publishing RPC message to '{Queue}' (corrId={CorrelationId}).", queue, correlationId);

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queue,
                mandatory: false,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(payload),
                cancellationToken: ct);

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            linkedCts.CancelAfter(TimeSpan.FromSeconds(_settings.Timeout));

            var completed = await Task.WhenAny(tcs.Task, Task.Delay(Timeout.InfiniteTimeSpan, linkedCts.Token));
            if (completed == tcs.Task)
            {
                return await tcs.Task.ConfigureAwait(false);
            }

            throw new GatewayTimeoutException(
                "TIMEOUT",
                $"No response received within the timeout period ({_settings.Timeout}s).");
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(consumerTag))
            {
                try
                {
                    await channel.BasicCancelAsync(consumerTag, cancellationToken: CancellationToken.None);
                }
                catch
                {
                    /* channel may already be closing */
                }
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync();
        try
        {
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }
        }
        finally
        {
            _gate.Release();
            _gate.Dispose();
        }
    }

    private async Task<IConnection> EnsureConnectionAsync(CancellationToken ct)
    {
        if (_connection is { IsOpen: true })
        {
            return _connection;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_connection is { IsOpen: true })
            {
                return _connection;
            }

            _logger.Information("Opening RabbitMQ connection to {Host}...", _factory.HostName);
            _connection = await _factory.CreateConnectionAsync(ct);
            _logger.Information("RabbitMQ connection established.");
            return _connection;
        }
        finally
        {
            _gate.Release();
        }
    }
}
