namespace Gateway.Configuration
{
    /// <summary>
    /// App settings representation class
    /// </summary>
    public sealed class AppSettings
    {
        /// <summary>
        /// Keycloak settings
        /// </summary>
        public required KeycloakSettings Keycloak { get; set; }

        /// <summary>
        /// Allowed origins
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }

    /// <summary>
    /// Keycloak settings representation class
    /// </summary>
    public sealed class KeycloakSettings
    {
        /// <summary>
        /// Keycloak authority
        /// </summary>
        public required string Authority { get; set; }

        /// <summary>
        /// Keycloak audience
        /// </summary>
        public required string Audience { get; set; }

        /// <summary>
        /// Require HTTPS metadata
        /// </summary>
        public bool RequireHttpsMetadata { get; set; }
    }
}
