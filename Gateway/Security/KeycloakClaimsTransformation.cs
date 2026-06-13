using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Gateway.Security
{
    /// <summary>
    /// Applies a Keycloak-specific transformation to flatten realm and client roles into standard <see cref="ClaimTypes.Role"/> claims.
    /// </summary>
    /// <remarks>
    /// This transformation reads Keycloak's JWT claims:
    /// <list type="bullet">
    /// <item><description><c>realm_access.roles</c> → adds roles as <c>realm:&lt;role&gt;</c></description></item>
    /// <item><description><c>resource_access.&lt;clientId&gt;.roles</c> → adds roles as <c>&lt;clientId&gt;:&lt;role&gt;</c></description></item>
    /// </list>
    /// A marker claim (<c>__kc_roles_flattened</c>) is used to ensure idempotency
    /// in case the transformation runs multiple times in the pipeline.
    /// </remarks>
    public sealed class KeycloakClaimsTransformation : IClaimsTransformation
    {
        private const string RealmAccess = "realm_access";
        private const string ResourceAccess = "resource_access";
        private const string Roles = "roles";
        private const string FlattenedMarkerType = "__kc_roles_flattened";
        private const string FlattenedMarkerValue = "1";

        /// <summary>
        /// Transforms the incoming <see cref="ClaimsPrincipal"/> by adding
        /// normalized role claims derived from Keycloak token structure.
        /// </summary>
        /// <param name="principal">The authenticated principal to transform.</param>
        /// <returns>
        /// The same <see cref="ClaimsPrincipal"/> instance with additional role claims,
        /// or the original principal if not authenticated or already processed.
        /// </returns>
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity id || !id.IsAuthenticated)
                return Task.FromResult(principal);

            // Avoid duplicating on multiple calls
            if (id.HasClaim(FlattenedMarkerType, FlattenedMarkerValue))
                return Task.FromResult(principal);

            // Create a quick set of existing role values to avoid accidental duplicates
            var existingRoles = new HashSet<string>(
                id.FindAll(ClaimTypes.Role).Select(c => c.Value),
                StringComparer.Ordinal);

            // 1) Realm roles -> add as "realm:<role>"
            var realmAccess = id.FindFirst(RealmAccess)?.Value;
            if (!string.IsNullOrWhiteSpace(realmAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(realmAccess);
                    if (doc.RootElement.TryGetProperty(Roles, out var roles) && roles.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var r in roles.EnumerateArray())
                        {
                            if (r.ValueKind == JsonValueKind.String)
                            {
                                var value = $"realm:{r.GetString()}";
                                if (existingRoles.Add(value))
                                    id.AddClaim(new Claim(ClaimTypes.Role, value));
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors: token may not contain expected JSON.
                }
            }

            // 2) Client roles -> add as "<clientId>:<role>"
            var resourceAccess = id.FindFirst(ResourceAccess)?.Value;
            if (!string.IsNullOrWhiteSpace(resourceAccess))
            {
                try
                {
                    using var doc = JsonDocument.Parse(resourceAccess);
                    foreach (var clientProp in doc.RootElement.EnumerateObject())
                    {
                        var clientId = clientProp.Name;
                        if (clientProp.Value.TryGetProperty(Roles, out var roles) && roles.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var r in roles.EnumerateArray())
                            {
                                if (r.ValueKind == JsonValueKind.String)
                                {
                                    var value = $"{clientId}:{r.GetString()}";
                                    if (existingRoles.Add(value))
                                        id.AddClaim(new Claim(ClaimTypes.Role, value));
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors: token may not contain expected JSON.
                }
            }

            // Marker to avoid repeated work
            id.AddClaim(new Claim(FlattenedMarkerType, FlattenedMarkerValue));

            return Task.FromResult(principal);
        }
    }
}
