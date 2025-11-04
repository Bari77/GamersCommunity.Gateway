using GamersCommunity.Core.Enums;

namespace Gateway.Abstractions
{
    /// <summary>
    /// Defines the contract for resolving routing and authorization rules 
    /// between microservices and their associated resources.
    /// </summary>
    public interface IGatewayRouter
    {
        /// <summary>
        /// Resolves the RabbitMQ queue name from the provided microservice identifier.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <returns>
        /// The resolved RabbitMQ queue name associated with the specified microservice.
        /// </returns>
        string? ResolveQueue(string ms);

        /// <summary>
        /// Resolves the service type from the provided microservice identifier and resource name.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <param name="resource">The resources</param>
        /// <returns>
        /// The resolved service type associated with the specified microservice and resource
        /// </returns>
        BusServiceTypeEnum ResolveType(string ms, string resource);

        /// <summary>
        /// Determines whether the specified resource is authorized for the given microservice.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <param name="resource">The name of the resource to validate.</param>
        /// <returns>
        /// <c>true</c> if the resource is authorized for the specified microservice otherwise, <c>false</c>.
        /// </returns>
        bool IsResourceAllowed(string ms, string resource);

        /// <summary>
        /// Determines whether the specified action is authorized on the given resource 
        /// for the specified microservice.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <param name="resource">The name of the resource on which the action is attempted.</param>
        /// <param name="action">The action to validate (e.g., read, write, delete).</param>
        /// <returns>
        /// <c>true</c> if the action is authorized on the resource for the specified microservice otherwise, <c>false</c>.
        /// </returns>
        bool IsActionAllowed(string ms, string resource, string action);

        /// <summary>
        /// Determines whether a specific resource (optionally action) is public or private.
        /// </summary>
        /// <param name="microservice">Microservice identifier (<c>ms</c>).</param>
        /// <param name="resource">Resource or resource name.</param>
        /// <param name="action">Optional action name (e.g., <c>List</c>, <c>Create</c>).</param>
        /// <returns><see langword="true"/> if the resource is public; otherwise <see langword="false"/>.</returns>
        bool IsPublic(string microservice, string resource, string? action = null);
    }
}
