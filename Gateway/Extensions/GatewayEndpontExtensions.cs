using Gateway.Abstractions;
using Microsoft.AspNetCore.Authentication;

namespace Gateway.Extensions
{
    public static class GatewayEndpointExtensions
    {
        public static RouteHandlerBuilder RequireAuthorizationIfNotPublic(
            this RouteHandlerBuilder builder,
            string? defaultAction = null)
        {
            builder.AddEndpointFilter(async (context, next) =>
            {
                var ms = context.GetArgument<string>(0);
                var table = context.GetArgument<string>(1);
                string? action = defaultAction;

                for (int i = 0; i < context.Arguments.Count; i++)
                {
                    if (context.Arguments[i] is string arg && i >= 2)
                    {
                        action = arg;
                        break;
                    }
                }

                var router = context.HttpContext.RequestServices.GetRequiredService<IGatewayRouter>();
                var isPublic = router.IsPublic(ms, table, action);

                var result = await context.HttpContext.AuthenticateAsync();
                if (!isPublic && !result.Succeeded)
                    return Results.Unauthorized();

                return await next(context);
            });

            return builder;
        }
    }
}
