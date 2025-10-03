namespace Gateway.Configuration
{
    /// <summary>
    /// Represents the routing options used by the gateway to control 
    /// microservice-to-queue mappings, allowed tables, and allowed actions.
    /// </summary>
    public sealed class GatewayRoutingSettings
    {
        /// <summary>
        /// Defines the mapping between a microservice identifier (<c>ms</c>) 
        /// and its corresponding RabbitMQ queue name.
        /// </summary>
        /// <remarks>
        /// Keys are microservice identifiers.  
        /// Values are the RabbitMQ queue names.  
        /// The comparison is case-insensitive.
        /// </remarks>
        public Dictionary<string, string> QueueByMS { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Defines the allowlist of tables per microservice (<c>ms</c>).
        /// </summary>
        /// <remarks>
        /// Keys are microservice identifiers.  
        /// Values are sets of table names that are allowed for the given microservice.  
        /// If a microservice is absent from the dictionary, no tables are authorized.
        /// The comparison is case-insensitive.
        /// </remarks>
        public Dictionary<string, HashSet<string>> AllowedTablesByMS { get; init; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Defines the allowlist of actions per resource (e.g., table or entity).
        /// </summary>
        /// <remarks>
        /// Keys are resource identifiers (e.g., table names).  
        /// Values are sets of allowed action names (e.g., <c>read</c>, <c>write</c>, <c>delete</c>).  
        /// The comparison is case-insensitive.
        /// </remarks>
        public Dictionary<string, HashSet<string>> AllowedActionsByResource { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
