using Gateway.Abstractions;
using Gateway.Configuration;
using Microsoft.Extensions.Options;

namespace Gateway.Core
{
    public sealed class GatewayRouter(IOptions<GatewayRoutingSettings> options) : IGatewayRouter
    {
        private readonly GatewayRoutingSettings _opts = options.Value;

        public string ResolveQueue(string game)
        {
            if (!_opts.QueueByGame.TryGetValue(game, out var queue))
                throw new KeyNotFoundException($"Unknown game '{game}'.");
            return queue;
        }

        public bool IsTableAllowed(string game, string table)
            => _opts.AllowedTablesByGame.TryGetValue(game, out var set) && set.Contains(table);

        public bool IsActionAllowed(string game, string table, string action)
            => _opts.AllowedActionsByResource.TryGetValue($"{game}/{table}", out var set) && set.Contains(action);
    }
}
