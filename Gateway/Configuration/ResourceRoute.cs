using GamersCommunity.Core.Enums;
using Gateway.Enums;

namespace Gateway.Configuration;

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
