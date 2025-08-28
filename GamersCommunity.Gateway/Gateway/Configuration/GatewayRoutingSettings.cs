namespace Gateway.Configuration
{

    /// <summary>
    /// Options de routage pour contrôler les jeux → queues et la liste blanche des tables.
    /// </summary>
    public sealed class GatewayRoutingSettings
    {
        /// <summary>
        /// Mapping {game} → nom de la queue RabbitMQ.
        /// </summary>
        public Dictionary<string, string> QueueByGame { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Liste blanche des tables par {game}. Si absent, aucune table n'est autorisée.
        /// </summary>
        public Dictionary<string, HashSet<string>> AllowedTablesByGame { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Allowlist des actions par table.
        /// </summary>
        public Dictionary<string, HashSet<string>> AllowedActionsByResource { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
