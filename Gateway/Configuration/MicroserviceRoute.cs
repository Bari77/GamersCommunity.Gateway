using Gateway.Enums;

namespace Gateway.Configuration;

/// <summary>
/// Represents a single microservice entry in the Gateway configuration.
/// </summary>
/// <remarks>
/// Defines the link between a microservice identifier (used in the URL path) 
/// and its corresponding message queue name in RabbitMQ.  
/// Contains nested resource definitions that describe which resources are exposed.
/// </remarks>
public sealed class MicroserviceRoute
{
    /// <summary>
    /// Unique identifier of the microservice.
    /// </summary>
    /// <remarks>
    /// This identifier corresponds to the <c>{ms}</c> segment of API routes (e.g. <c>/api/{ms}/{resource}</c>).
    /// </remarks>
    public required string Id { get; init; }

    /// <summary>
    /// Name of the RabbitMQ queue associated with this microservice.
    /// </summary>
    /// <remarks>
    /// All requests targeting this microservice will be routed through the specified queue.
    /// </remarks>
    public required string Queue { get; init; }

    /// <summary>
    /// List of resource definitions exposed by this microservice.
    /// </summary>
    /// <remarks>
    /// Each resource defines its own scope and set of allowed actions.
    /// </remarks>
    public List<ResourceRoute> Resources { get; init; } = [];

    /// <summary>
    /// Default access scope for this microservice.
    /// </summary>
    /// <remarks>
    /// When a resource or action does not explicitly define a scope, 
    /// it inherits the one from its parent microservice.
    /// </remarks>
    public AccessScope Scope { get; init; } = AccessScope.Private;
}
