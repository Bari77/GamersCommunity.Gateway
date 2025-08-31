using GamersCommunity.Core.Rabbit;
using Gateway.Abstractions;
using Gateway.Core;
using Gateway.Security;
using Microsoft.AspNetCore.Authentication;
using Serilog;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Gateway.Endpoints
{
    public static class GatewayEndpoints
    {
        private static readonly JsonSerializerOptions JsonOpts = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public static IServiceCollection AddGatewayServices(this IServiceCollection services)
        {
            // Options + services
            services.AddOptions();

            services.AddScoped<IClaimsTransformation, KeycloakClaimsTransformation>();

            services.AddSingleton<Serilog.ILogger>(sp => Log.Logger);

            services.AddScoped<RabbitMQProducer>();
            services.AddScoped<IRabbitRpcClient, RabbitRpcClient>();

            services.AddScoped<IGatewayRouter, GatewayRouter>();
            return services;
        }

        /// <summary>
        /// Mappe les endpoints génériques de la gateway.
        /// </summary>
        public static IEndpointRouteBuilder MapGatewayEndpoints(this IEndpointRouteBuilder app)
        {
            // POST /api/{game}/{table} -> Create
            app.MapPost("/api/{game}/{table}", async (
                string game,
                string table,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");

                var queue = router.ResolveQueue(game);
                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = "Create",
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var id = await rpc.CallAsync(queue, payload, ct);

                return Results.Created($"/api/{game}/{table}/{id}", id);
            }).RequireAuthorization();

            // GET /api/{game}/{table} -> List
            app.MapGet("/api/{game}/{table}", async (
                string game,
                string table,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");

                var queue = router.ResolveQueue(game);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = "List"
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Ok(result);
            }).RequireAuthorization();

            // GET /api/{game}/{table}/{id:int} -> Get
            app.MapGet("/api/{game}/{table}/{id:int}", async (
                string game,
                string table,
                int id,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");

                var queue = router.ResolveQueue(game);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = "Get",
                    Data = id.ToString()
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var result = await rpc.CallAsync(queue, payload, ct);
                return Results.Ok(result);
            }).RequireAuthorization();

            // PUT /api/{game}/{table}/{id:int} -> Update
            app.MapPut("/api/{game}/{table}/{id:int}", async (
                string game,
                string table,
                int id,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");

                var queue = router.ResolveQueue(game);
                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = "Update",
                    Id = id,
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorization();

            // DELETE /api/{game}/{table}/{id:int} -> Delete
            app.MapDelete("/api/{game}/{table}/{id:int}", async (
                string game,
                string table,
                int id,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");

                var queue = router.ResolveQueue(game);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = "Delete",
                    Data = id.ToString()
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                await rpc.CallAsync(queue, payload, ct);
                return Results.NoContent();
            }).RequireAuthorization();

            app.MapPost("/api/{game}/{table}/actions/{action}", async (
                string game,
                string table,
                string action,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");
                if (!router.IsActionAllowed(game, table, action)) return Results.NotFound($"Unknown action '{action}' for '{game}/{table}'.");

                var queue = router.ResolveQueue(game);
                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = action,
                    Data = jsonBody,
                    Id = null
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var response = await rpc.CallAsync(queue, payload, ct);
                return Results.Ok(response);
            }).RequireAuthorization();

            app.MapPost("/api/{game}/{table}/{id:int}/actions/{action}", async (
                string game,
                string table,
                int id,
                string action,
                HttpRequest req,
                IGatewayRouter router,
                IRabbitRpcClient rpc,
                CancellationToken ct) =>
            {
                if (!router.IsTableAllowed(game, table)) return Results.NotFound($"Unknown table '{table}' for game '{game}'.");
                if (!router.IsActionAllowed(game, table, action)) return Results.NotFound($"Unknown action '{action}' for '{game}/{table}'.");

                var queue = router.ResolveQueue(game);
                var jsonBody = await new StreamReader(req.Body).ReadToEndAsync(ct);

                var msg = new RabbitMQTableMessage
                {
                    Table = table,
                    Action = action,
                    Id = id,
                    Data = jsonBody
                };

                var payload = JsonSerializer.Serialize(msg, JsonOpts);
                var response = await rpc.CallAsync(queue, payload, ct);
                return Results.Ok(response);
            }).RequireAuthorization();

            return app;
        }
    }
}
