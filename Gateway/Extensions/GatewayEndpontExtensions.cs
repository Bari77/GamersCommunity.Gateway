using Gateway.Abstractions;
using Microsoft.AspNetCore.Authentication;

namespace Gateway.Extensions
{
    /// <summary>
    /// Provides reusable endpoint extension methods for conditional authorization based on <see cref="GatewayRouter"/>.
    /// </summary>
    public static class GatewayEndpointExtensions
    {
        /// <summary>
        /// Conditionally enforces authentication based on the resource's access scope (public or private).
        /// </summary>
        /// <remarks>
        /// If the target microservice, table, or action is declared as <c>Public</c> in configuration,
        /// authentication is skipped; otherwise a valid JWT token is required.
        /// </remarks>
        public static RouteHandlerBuilder RequireAuthorizationIfNotPublic(
            this RouteHandlerBuilder builder,
            string? defaultAction = null)
        {
            builder.AddEndpointFilter(async (context, next) =>
            {
                var ms = context.GetArgument<string>(0);
                var table = context.GetArgument<string>(1);
                string? action = defaultAction;

                // Si la route contient une action dans les arguments, on la prend
                for (int i = 0; i < context.Arguments.Count; i++)
                {
                    if (context.Arguments[i] is string arg && i >= 2)
                    {
                        action = arg;
                        break;
                    }
                }

                var router = context.HttpContext.RequestServices.GetRequiredService<IGatewayRouter>();

                if (router.IsPublic(ms, table, action))
                    return await next(context);

                var result = await context.HttpContext.AuthenticateAsync();
                if (!result.Succeeded)
                    return Results.Unauthorized();

                return await next(context);
            });

            return builder;
        }

    }
}
