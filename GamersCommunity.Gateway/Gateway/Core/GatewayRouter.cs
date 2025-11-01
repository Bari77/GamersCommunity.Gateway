using Gateway.Abstractions;
using Gateway.Configuration;
using Gateway.Enums;
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
        private readonly GatewayRoutingSettings _settings = options.Value;

        /// <inheritdoc/>
        public string? ResolveQueue(string microservice) =>
            _settings.Microservices
                .FirstOrDefault(x => x.Id.Equals(microservice, StringComparison.OrdinalIgnoreCase))
                ?.Queue;

        /// <inheritdoc/>
        public bool IsTableAllowed(string microservice, string table)
        {
            var ms = _settings.Microservices
                .FirstOrDefault(x => x.Id.Equals(microservice, StringComparison.OrdinalIgnoreCase));

            if (ms is null) return false;
            return ms.Tables.Any(t => t.Name.Equals(table, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public bool IsActionAllowed(string microservice, string table, string action)
        {
            var tbl = GetTable(microservice, table);
            if (tbl is null) return false;

            // If no actions defined → all are considered allowed
            if (tbl.Actions.Count == 0) return true;

            return tbl.Actions.Any(a => a.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public bool IsPublic(string microservice, string table, string? action = null)
        {
            var ms = GetMicroservice(microservice);
            if (ms is null) return false; // unknown microservice → always private

            var tbl = GetTable(microservice, table);
            if (tbl is null) return false; // table not declared → private

            // Look for explicit action override
            if (!string.IsNullOrEmpty(action))
            {
                var act = tbl.Actions.FirstOrDefault(a =>
                    a.Name.Equals(action, StringComparison.OrdinalIgnoreCase));

                if (act?.Scope != null)
                    return act.Scope == AccessScope.Public;
            }

            // Inherit from table → microservice
            var tableScope = tbl.Scope ?? ms.Scope;
            return tableScope == AccessScope.Public;
        }

        private MicroserviceRoute? GetMicroservice(string id) =>
            _settings.Microservices.FirstOrDefault(
                x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

        private TableRoute? GetTable(string ms, string table) =>
            GetMicroservice(ms)?.Tables.FirstOrDefault(
                t => t.Name.Equals(table, StringComparison.OrdinalIgnoreCase));
    }
}
