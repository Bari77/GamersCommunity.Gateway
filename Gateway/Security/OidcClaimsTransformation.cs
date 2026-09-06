using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Text.Json;

namespace Gateway.Security
{
    public sealed class OidcClaimsTransformation : IClaimsTransformation
    {
        private const string RealmAccess = "realm_access";
        private const string ResourceAccess = "resource_access";
        private const string Roles = "roles";
        private const string Groups = "groups";
        private const string FlattenedMarkerType = "__oidc_roles_flattened";
        private const string FlattenedMarkerValue = "1";

        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
        {
            if (principal.Identity is not ClaimsIdentity source || !source.IsAuthenticated)
                return Task.FromResult(principal);

            if (source.HasClaim(FlattenedMarkerType, FlattenedMarkerValue))
                return Task.FromResult(principal);

            // Clone: auth may reuse the same identity across concurrent requests,
            // and ClaimsIdentity forbids AddClaim while FindAll is enumerating.
            var id = source.Clone();

            var existingRoles = new HashSet<string>(
                id.FindAll(ClaimTypes.Role).Select(c => c.Value),
                StringComparer.Ordinal);

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
                }
            }

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
                }
            }

            foreach (var groupClaim in id.FindAll(Groups).ToList())
            {
                var raw = groupClaim.Value;
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                if (raw.StartsWith('['))
                {
                    try
                    {
                        using var doc = JsonDocument.Parse(raw);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var g in doc.RootElement.EnumerateArray())
                            {
                                if (g.ValueKind == JsonValueKind.String)
                                {
                                    var value = g.GetString();
                                    if (!string.IsNullOrWhiteSpace(value) && existingRoles.Add(value!))
                                        id.AddClaim(new Claim(ClaimTypes.Role, value!));
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
                else if (existingRoles.Add(raw))
                {
                    id.AddClaim(new Claim(ClaimTypes.Role, raw));
                }
            }

            id.AddClaim(new Claim(FlattenedMarkerType, FlattenedMarkerValue));

            return Task.FromResult(new ClaimsPrincipal(id));
        }
    }
}
