using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Models;
using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Concurrent;
using System.Text.Json;

namespace Gateway.Health
{
    /// <summary>
    /// Performs a distributed health check by querying all registered microservices
    /// through RabbitMQ and aggregating their responses into a single report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// For each microservice registered in the gateway routing configuration,
    /// this class sends a <see cref="BusMessage"/> with <c>Type = INFRA</c>,
    /// <c>Resource = "Health"</c>, and <c>Action = "Check"</c>.
    /// </para>
    /// <para>
    /// Each microservice should respond with a serialized <see cref="MicroserviceHealth"/>
    /// object describing its overall and database status.
    /// </para>
    /// <para>
    /// The aggregated result is exposed through the ASP.NET Core health-check pipeline
    /// (e.g., <c>/api/health</c> endpoint) for observability and monitoring.
    /// </para>
    /// </remarks>
    /// <param name="router">Gateway router providing access to registered microservices and queues.</param>
    /// <param name="rpc">RabbitMQ RPC client used to send health-check messages.</param>
    public sealed class MicroservicesHealthCheck(IGatewayRouter router, IRabbitRpcClient rpc) : IHealthCheck
    {
        /// <summary>
        /// Json options
        /// </summary>
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNameCaseInsensitive = true
        };

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken = default)
        {
            var data = new ConcurrentDictionary<string, object>();
            var overall = HealthStatus.Healthy;

            await Parallel.ForEachAsync(router.GetRegisteredMicroservices(), cancellationToken, async (msId, ct) =>
            {
                var msHealth = new MicroserviceHealth
                {
                    Status = HealthStatus.Healthy
                };

                try
                {
                    var queue = router.ResolveQueue(msId);
                    if (string.IsNullOrWhiteSpace(queue))
                        throw new Exception("Queue not found");

                    var msg = new BusMessage
                    {
                        Type = BusServiceTypeEnum.INFRA,
                        Resource = "Health",
                        Action = "Check"
                    };

                    var payload = JsonSerializer.Serialize(msg);

                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(2));

                    var response = await rpc.CallAsync(queue, payload, cts.Token);
                    msHealth = JsonSerializer.Deserialize<MicroserviceHealth>(response, JsonOpts)
                        ?? throw new Exception("Invalid health response");
                }
                catch (Exception)
                {
                    msHealth.Status = HealthStatus.Unhealthy;
                }

                data[msId] = msHealth;

                if (msHealth.Status == HealthStatus.Unhealthy)
                    Interlocked.Exchange(ref overall, HealthStatus.Unhealthy);
            });

            return overall switch
            {
                HealthStatus.Healthy => HealthCheckResult.Healthy("All microservices healthy", data),
                _ => HealthCheckResult.Unhealthy("One or more microservices unhealthy", null, data)
            };
        }
    }
}
