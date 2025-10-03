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
        string ResolveQueue(string ms);

        /// <summary>
        /// Determines whether the specified table is authorized for the given microservice.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <param name="table">The name of the table to validate.</param>
        /// <returns>
        /// <c>true</c> if the table is authorized for the specified microservice otherwise, <c>false</c>.
        /// </returns>
        bool IsTableAllowed(string ms, string table);

        /// <summary>
        /// Determines whether the specified action is authorized on the given table 
        /// for the specified microservice.
        /// </summary>
        /// <param name="ms">The identifier of the microservice.</param>
        /// <param name="table">The name of the table on which the action is attempted.</param>
        /// <param name="action">The action to validate (e.g., read, write, delete).</param>
        /// <returns>
        /// <c>true</c> if the action is authorized on the table for the specified microservice otherwise, <c>false</c>.
        /// </returns>
        bool IsActionAllowed(string ms, string table, string action);
    }
}
