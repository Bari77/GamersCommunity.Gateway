using GamersCommunity.Core.Enums;
using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;
using Gateway.Core;
using Gateway.Extensions;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Endpoints
{
    /// <summary>
    /// Provides extension methods for configuring and registering 
    /// gateway-related services in the dependency injection container.
    /// </summary>
    public static class GatewayEndpoints
    {
        /// <summary>
        /// Default JSON serializer options used across the gateway.  
        /// Configured with camelCase naming policy and ignores null values when writing.
        /// </summary>
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        /// <summary>
        /// Registers all required gateway services into the dependency injection container.
        /// </summary>
        public static IServiceCollection AddGatewayServices(this IServiceCollection services)
        {
            services.AddOptions();
            services.AddScoped<IClaimsTransformation, OidcClaimsTransformation>();
            services.AddSingleton<Serilog.ILogger>(sp => Log.Logger);
            services.AddSingleton<RabbitMQProducer>();
            services.AddSingleton<IRabbitRpcClient, RabbitRpcClient>();
            services.AddSingleton<IGatewayRouter, GatewayRouter>();
            return services;
        }

        /// <summary>
        /// Maps the generic gateway endpoints.
        /// <param name="app">The app</param>
        /// </summary>
        public static IEndpointRouteBuilder MapGatewayEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapHealthChecks("/api/health", new HealthCheckOptions
            {
                Predicate = _ => true,

                ResultStatusCodes =
                {
                    [HealthStatus.Healthy]  = StatusCodes.Status200OK,
                    [HealthStatus.Degraded] = StatusCodes.Status200OK,
                    [HealthStatus.Unhealthy]= StatusCodes.Status503ServiceUnavailable,
                },

                ResponseWriter = async (ctx, report) =>
                {
                    ctx.Response.ContentType = "application/json";

                    var body = new
                    {
                        Status = report.Status.ToString(),
                        Checks = report.Entries.Select(e => new
                        {
                            Name = e.Key,
                            Status = e.Value.Status.ToString(),
                            Data = e.Value.Data
                        })
                    };

                    await ctx.Response.WriteAsync(JsonSerializer.Serialize(body));
                }
            });

            app.MapPost("/api/{ms}/{resource}", async (
                string ms,
                string resource,
                HttpRequest req,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, "Create", jsonBody);

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var id = await rpc.CallAsync(queue, payload, ct);

                return Results.Created($"/api/{ms}/{resource}/{id}", id);
            }).RequireAuthorizationIfNotPublic("Create");

            app.MapGet("/api/{ms}/{resource}", async (
                string ms,
                string resource,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, "List");

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic("List");

            app.MapGet("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, "Get", id.ToString());

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic("Get");

            app.MapPut("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                HttpRequest req,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, "Update", jsonBody, id);

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorizationIfNotPublic("Update");

            app.MapDelete("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, "Delete", id.ToString());

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorizationIfNotPublic("Delete");

            app.MapPost("/api/{ms}/{resource}/actions/{action}", async (
                string ms,
                string resource,
                string action,
                HttpRequest req,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();
                if (!router.IsActionAllowed(ms, resource, action)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, action, jsonBody);

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic();

            app.MapPost("/api/{ms}/{resource}/{id:int}/actions/{action}", async (
                string ms,
                string resource,
                int id,
                string action,
                HttpRequest req,
                HttpContext http,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();
                if (!router.IsActionAllowed(ms, resource, action)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = CreateBusMessage(http.User, router.ResolveType(ms, resource), resource, action, jsonBody, id);

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic();

            return app;
        }

        private static BusMessage CreateBusMessage(
            ClaimsPrincipal user,
            BusServiceTypeEnum type,
            string resource,
            string action,
            string? data = null,
            int? id = null) =>
            new()
            {
                Type = type,
                Resource = resource,
                Action = action,
                Data = data,
                Id = id,
                Caller = CreateCaller(user)
            };

        private static CallerIdentity? CreateCaller(ClaimsPrincipal user)
        {
            if (user.Identity is not { IsAuthenticated: true })
                return null;

            return new CallerIdentity
            {
                Subject = user.FindFirst("sub")?.Value
                    ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                Email = user.FindFirst("email")?.Value
                    ?? user.FindFirst(ClaimTypes.Email)?.Value,
                Username = user.FindFirst("preferred_username")?.Value
                    ?? user.FindFirst("name")?.Value
                    ?? user.Identity.Name,
                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray()
            };
        }
    }
}
