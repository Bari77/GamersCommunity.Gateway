namespace Gateway.Enums
{
    /// <summary>
    /// Enumerates the possible access scopes that can be applied to 
    /// a microservice, table, or action definition.
    /// </summary>
    public enum AccessScope
    {
        /// <summary>
        /// Indicates that the resource or action can be accessed without authentication.
        /// </summary>
        Public,

        /// <summary>
        /// Indicates that authentication is required to access the resource or action.
        /// </summary>
        Private
    }
}
