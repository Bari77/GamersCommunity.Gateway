using Gateway.Enums;

namespace Gateway.Configuration;

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
