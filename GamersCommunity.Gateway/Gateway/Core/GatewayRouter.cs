using Gateway.Abstractions;
using Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Gateway.Core
{
    /// <summary>
    /// Default implementation of <see cref="IGatewayRouter"/> that resolves
    /// microservice routing rules based on <see cref="GatewayRoutingSettings"/>.
    /// </summary>
    /// <remarks>
    /// This router uses the configuration provided by <see cref="GatewayRoutingSettings"/> to:
    /// <list type="bullet">
    /// <item><description>Resolve RabbitMQ queue names for microservices.</description></item>
    /// <item><description>Check whether a table is authorized for a given microservice.</description></item>
    /// <item><description>Check whether an action is authorized for a specific table and microservice.</description></item>
    /// </list>
    /// </remarks>
    public sealed class GatewayRouter(IOptions<GatewayRoutingSettings> options) : IGatewayRouter
    {
        private readonly GatewayRoutingSettings _opts = options.Value;

        /// <inheritdoc />
        /// <exception cref="KeyNotFoundException">
        /// Thrown when the specified microservice does not exist in the routing configuration.
        /// </exception>
        public string ResolveQueue(string ms)
        {
            if (!_opts.QueueByMS.TryGetValue(ms, out var queue))
                throw new KeyNotFoundException($"Unknown microservice '{ms}'.");
            return queue;
        }

        /// <inheritdoc />
        public bool IsTableAllowed(string ms, string table)
            => _opts.AllowedTablesByMS.TryGetValue(ms, out var set) && set.Contains(table);

        /// <inheritdoc />
        public bool IsActionAllowed(string ms, string table, string action)
            => _opts.AllowedActionsByResource.TryGetValue($"{ms}/{table}", out var set) && set.Contains(action);
    }
}
