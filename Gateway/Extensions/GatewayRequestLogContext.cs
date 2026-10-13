namespace Gateway.Extensions;

internal static class GatewayRequestLogContext
{
    public static GatewayRequestLogContextData From(HttpContext context)
    {
        var route = context.GetRouteData()?.Values;
        var ms = ReadRoute(route, "ms");
        var resource = ReadRoute(route, "resource");
        var action = ReadRoute(route, "action");
        var id = ReadRoute(route, "id");
        var publicId = ReadRoute(route, "publicId");

        return new GatewayRequestLogContextData(
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            ms,
            resource,
            action,
            id,
            publicId);
    }

    private static string? ReadRoute(RouteValueDictionary? route, string key) =>
        route is not null && route.TryGetValue(key, out var value) ? value?.ToString() : null;
}
