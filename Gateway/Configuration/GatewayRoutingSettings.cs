using Gateway.Enums;

namespace Gateway.Configuration;

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
