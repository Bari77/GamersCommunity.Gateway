using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Gateway.Security
{
    public sealed class KeycloakClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity id || !id.IsAuthenticated)
                return Task.FromResult(principal);

            // Avoid duplicating on multiple calls
            if (id.HasClaim("__kc_roles_flattened", "1"))
                return Task.FromResult(principal);

            // 1) Realm roles -> add as "realm:<role>"
            var realmAccess = id.FindFirst("realm_access")?.Value;
            if (!string.IsNullOrWhiteSpace(realmAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(realmAccess);
                    if (doc.RootElement.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in roles.EnumerateArray())
                            if (r.ValueKind == JsonValueKind.String)
                                id.AddClaim(new Claim(ClaimTypes.Role, $"realm:{r.GetString()}"));
                    }
                }
                catch { /* ignore parse errors */ }
            }

            // 2) Client roles -> add as "<clientId>:<role>"
            var resourceAccess = id.FindFirst("resource_access")?.Value;
            if (!string.IsNullOrWhiteSpace(resourceAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(resourceAccess);
                    foreach (var clientProp in doc.RootElement.EnumerateObject())
                    {
                        var clientId = clientProp.Name;
                        if (clientProp.Value.TryGetProperty("roles", out var roles) && roles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var r in roles.EnumerateArray())
                                if (r.ValueKind == JsonValueKind.String)
                                    id.AddClaim(new Claim(ClaimTypes.Role, $"{clientId}:{r.GetString()}"));
                        }
                    }
                }
                catch { /* ignore parse errors */ }
            }

            // Marker to avoid repeated work
            id.AddClaim(new Claim("__kc_roles_flattened", "1"));
            return Task.FromResult(principal);
        }
    }
}
