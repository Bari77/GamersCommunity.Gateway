namespace Gateway.Configuration
{
    /// <summary>
    /// App settings representation class
    /// </summary>
    public sealed class AppSettings
    {
        public required OidcSettings Oidc { get; set; }

        /// <summary>
        /// Allowed origins
        /// </summary>
        public string[] AllowedOrigins { get; set; } = [];
    }

    public sealed class OidcSettings
    {
        public required string Authority { get; set; }

        public required string Audience { get; set; }

        public bool RequireHttpsMetadata { get; set; }
    }
}
