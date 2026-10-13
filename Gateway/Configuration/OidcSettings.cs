namespace Gateway.Configuration;

public sealed class OidcSettings
{
    public required string Authority { get; set; }

    public required string Audience { get; set; }

    public bool RequireHttpsMetadata { get; set; }
}
