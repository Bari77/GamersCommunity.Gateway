using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Exceptions;
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
    /// <item><description>Check whether a resource is authorized for a given microservice.</description></item>
    /// <item><description>Check whether an action is authorized for a specific resource and microservice.</description></item>
    /// </list>
    /// </remarks>
    public sealed class GatewayRouter(IOptions<GatewayRoutingSettings> options) : IGatewayRouter
    {
        private readonly GatewayRoutingSettings _settings = options.Value;

        /// <inheritdoc/>
        public string? ResolveQueue(string microservice) => GetMicroservice(microservice)!.Queue;

        /// <inheritdoc/>
        public BusServiceTypeEnum ResolveType(string microservice, string resource) => GetResource(microservice, resource)!.Type;

        /// <inheritdoc/>
        public bool IsResourceAllowed(string microservice, string resource) => GetMicroservice(microservice)!.Resources.Any(r => r.Name.Equals(resource, StringComparison.OrdinalIgnoreCase));

        /// <inheritdoc/>
        public bool IsActionAllowed(string microservice, string resource, string action)
        {
            var res = GetResource(microservice, resource, false);
            if (res is null) return false;

            if (res.Actions.Count == 0) return true;

            return res.Actions.Any(a => a.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
        }

        /// <inheritdoc/>
        public bool IsPublic(string microservice, string resource, string? action = null)
        {
            var ms = GetMicroservice(microservice, false);
            if (ms is null) return false; // unknown microservice → always private

            var res = GetResource(microservice, resource, false);
            if (resource is null) return false; // table not declared → private

            // Look for explicit action override
            if (!string.IsNullOrEmpty(action))
            {
                var act = res!.Actions.FirstOrDefault(a =>
                    a.Name.Equals(action, StringComparison.OrdinalIgnoreCase));

                if (act?.Scope != null)
                    return act.Scope == AccessScope.Public;
            }

            // Inherit from table → microservice
            var resScope = res!.Scope ?? ms.Scope;
            return resScope == AccessScope.Public;
        }

        private MicroserviceRoute? GetMicroservice(string id, bool throwIfNotFound = true)
        {
            var ms = _settings.Microservices.FirstOrDefault(x => x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));

            if (ms == null && throwIfNotFound)
            {
                throw new NotFoundException(message: "Microservice not found.");
            }

            return ms;
        }

        private ResourceRoute? GetResource(string ms, string resource, bool throwIfNotFound = true)
        {
            var res = GetMicroservice(ms, throwIfNotFound)!.Resources.FirstOrDefault(r => r.Name.Equals(resource, StringComparison.OrdinalIgnoreCase));

            if (res == null && throwIfNotFound)
            {
                throw new NotFoundException(message: "Resource not found.");
            }

            return res;
        }
    }
}
