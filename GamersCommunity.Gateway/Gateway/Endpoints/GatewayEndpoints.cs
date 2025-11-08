using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;
using Gateway.Core;
using Gateway.Extensions;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog;
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
        /// <param name="services">
        /// The service collection to which the gateway services will be added.
        /// </param>
        /// <returns>
        /// The updated <see cref="IServiceCollection"/> with all gateway services registered.
        /// </returns>
        /// <remarks>
        /// This method configures the following:
        /// <list type="bullet">
        /// <item><description>Options support for dependency injection.</description></item>
        /// <item><description>Keycloak claims transformation for authentication.</description></item>
        /// <item><description>Serilog logger as a singleton instance.</description></item>
        /// <item><description>RabbitMQ producer and RPC client for messaging.</description></item>
        /// <item><description>Gateway router for queue and authorization resolution.</description></item>
        /// </list>
        /// </remarks>
        public static IServiceCollection AddGatewayServices(this IServiceCollection services)
        {
            // Options + services
            services.AddOptions();

            // Authentication claims transformation (Keycloak)
            services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformation>();

            // Serilog logger instance
            services.AddSingleton<Serilog.ILogger>(sp => Log.Logger);

            // Messaging services
            services.AddScoped<RabbitMQProducer>();
            services.AddScoped<IRabbitRpcClient, RabbitRpcClient>();

            // Gateway router service
            services.AddScoped<IGatewayRouter, GatewayRouter>();
            return services;
        }

        /// <summary>
        /// Mappe les endpoints génériques de la gateway.
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

            // POST /api/{ms}/{resource} -> Create
            app.MapPost("/api/{ms}/{resource}", async (
                string ms,
                string resource,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = "Create",
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var id = await rpc.CallAsync(queue, payload, ct);

                return Results.Created($"/api/{ms}/{resource}/{id}", id);
            }).RequireAuthorizationIfNotPublic("Create");

            // GET /api/{ms}/{resource} -> List
            app.MapGet("/api/{ms}/{resource}", async (
                string ms,
                string resource,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = "List"
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic("List");

            // GET /api/{ms}/{resource}/{id:int} -> Get by ID
            app.MapGet("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = "Get",
                    Data = id.ToString()
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic("Get");

            // PUT /api/{ms}/{resource}/{id:int} -> Update
            app.MapPut("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = "Update",
                    Id = id,
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorizationIfNotPublic("Update");

            // DELETE /api/{ms}/{resource}/{id:int} -> Delete
            app.MapDelete("/api/{ms}/{resource}/{id:int}", async (
                string ms,
                string resource,
                int id,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = "Delete",
                    Data = id.ToString()
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorizationIfNotPublic("Delete");

            // POST /api/{ms}/{resource}/actions/{action} -> Post custom action
            app.MapPost("/api/{ms}/{resource}/actions/{action}", async (
                string ms,
                string resource,
                string action,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();
                if (!router.IsActionAllowed(ms, resource, action)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = action,
                    Data = jsonBody,
                    Id = null
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic();

            // POST /api/{ms}/{resource}/{id:int}/actions/{action} -> Post custom action to ID
            app.MapPost("/api/{ms}/{resource}/{id:int}/actions/{action}", async (
                string ms,
                string resource,
                int id,
                string action,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsResourceAllowed(ms, resource)) return Results.Unauthorized();
                if (!router.IsActionAllowed(ms, resource, action)) return Results.Unauthorized();

                var queue = router.ResolveQueue(ms);
                if (queue is null) return Results.BadRequest("Unknown microservice.");

                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);
                var msg = new BusMessage
                {
                    Type = router.ResolveType(ms, resource),
                    Resource = resource,
                    Action = action,
                    Id = id,
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Text(result, "application/json");
            }).RequireAuthorizationIfNotPublic();

            return app;
        }
    }
}
