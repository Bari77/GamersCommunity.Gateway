using GamersCommunity.Core.Enums;
using Gateway.Enums;

namespace Gateway.Configuration
{
    /// <summary>
    /// Represents the hierarchical routing configuration for the Gateway, 
    /// defining microservices, their resources, actions, and associated access scopes.
    /// </summary>
    /// <remarks>
    /// This configuration enables fine-grained access control by allowing the specification of 
    /// <see cref="AccessScope"/> (public or private) at multiple levels:
    /// <list type="bullet">
    ///   <item><description><b>Microservice</b> → Global default scope for all its resources.</description></item>
    ///   <item><description><b>Resource</b> → Resource-level scope overriding the microservice default.</description></item>
    ///   <item><description><b>Action</b> → Per-operation scope overriding the resource scope.</description></item>
    /// </list>
    /// Scopes are inherited when unspecified, allowing concise yet expressive configurations.
    /// </remarks>
    public sealed class GatewayRoutingSettings
    {
        /// <summary>
        /// List of microservice route definitions that the Gateway can forward requests to.
        /// </summary>
        /// <remarks>
        /// Each entry defines routing, authorization, and queue binding details for a microservice.
        /// </remarks>
        public List<MicroserviceRoute> Microservices { get; init; } = [];
    }

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

    /// <summary>
    /// Represents a resource exposed by a given microservice.
    /// </summary>
    /// <remarks>
    /// Defines whether the resource is publicly accessible or requires authentication, 
    /// and optionally specifies custom scopes for each individual action.
    /// </remarks>
    public sealed class ResourceRoute
    {
        /// <summary>
        /// Name of the resource.
        /// </summary>
        /// <remarks>
        /// This corresponds to the <c>{resource}</c> segment in the API route (<c>/api/{ms}/{resource}</c>).
        /// </remarks>
        public required string Name { get; init; }

        /// <summary>
        /// Type of the resource.
        /// </summary>
        public required BusServiceTypeEnum Type { get; init; }

        /// <summary>
        /// Access scope explicitly assigned to this resource.
        /// </summary>
        /// <remarks>
        /// When set to <see langword="null"/>, the scope inherits from the parent microservice.
        /// </remarks>
        public AccessScope? Scope { get; init; }

        /// <summary>
        /// List of action definitions associated with this resource.
        /// </summary>
        /// <remarks>
        /// Each action represents a specific operation (e.g., <c>List</c>, <c>Create</c>, <c>Delete</c>) 
        /// and may define its own scope.
        /// </remarks>
        public List<ActionRoute> Actions { get; init; } = [];
    }

    /// <summary>
    /// Represents a single operation (action) on a resource, such as Create, List, Update, or Delete.
    /// </summary>
    /// <remarks>
    /// Each action can override the access scope of its parent resource, allowing per-operation control.
    /// </remarks>
    public sealed class ActionRoute
    {
        /// <summary>
        /// Name of the action (e.g., <c>List</c>, <c>Create</c>, <c>Update</c>, <c>Delete</c>).
        /// </summary>
        /// <remarks>
        /// Case-insensitive when matching against API route actions.
        /// </remarks>
        public required string Name { get; init; }

        /// <summary>
        /// Optional access scope specific to this action.
        /// </summary>
        /// <remarks>
        /// When <see langword="null"/>, the scope inherits from the parent resource or microservice.
        /// </remarks>
        public AccessScope? Scope { get; init; }
    }
}
