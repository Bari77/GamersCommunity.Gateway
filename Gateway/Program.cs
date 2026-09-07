using GamersCommunity.Core.Logging;
using GamersCommunity.Core.Rabbit;
using Gateway.Configuration;
using Gateway.Endpoints;
using Gateway.Health;
using Gateway.Hubs;
using Gateway.Middlewares;
using Gateway.Realtime;
using Gateway.Serialization;
using Gateway.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace APIGateway
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Console.Title = "Gateway";

            var builder = WebApplication.CreateBuilder(args);

            try
            {
                #region Initialize app settings

                #region Options

                builder.Services.AddOptions<LoggerSettings>().Bind(builder.Configuration.GetSection("LoggerSettings")).ValidateOnStart();
                var loggerSettings = builder.Configuration.GetSection("LoggerSettings").Get<LoggerSettings>() ?? new LoggerSettings();

                builder.Services.AddOptions<RabbitMQSettings>().Bind(builder.Configuration.GetSection("RabbitMQ")).ValidateOnStart();

                builder.Services.AddOptions<AppSettings>().Bind(builder.Configuration.GetSection("AppSettings")).ValidateOnStart();
                var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>()!;

                builder.Services.AddOptions<GatewayRoutingSettings>().Bind(builder.Configuration.GetSection("GatewayRouting")).ValidateOnStart();
                builder.Services.AddSingleton<IValidateOptions<GatewayRoutingSettings>, GatewayRoutingValidator>();

                #endregion

                #region Other

                builder.Services.Configure<ForwardedHeadersOptions>(o =>
                {
                    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                });

                #endregion

                #endregion

                #region Init Logger

                Logger.Initialize(loggerSettings, "Gateway", builder.Environment);
                // Clear all default logging providers, if you need to print in console,
                // http headers or DB queries, comment this line
                builder.Logging.ClearProviders();

                #endregion

                Log.Information("Starting ...");

                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(o =>
                    {
                        var oidc = appSettings.Oidc;

                        var authority = oidc.Authority.TrimEnd('/') + "/";
                        o.Authority = authority;
                        o.Audience = oidc.Audience;
                        o.MetadataAddress = $"{authority}.well-known/openid-configuration";
                        o.RequireHttpsMetadata = oidc.RequireHttpsMetadata;

                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = authority,

                            ValidateAudience = true,
                            ValidAudiences = ["account", "gc-front", "gc-gateway-api"],

                            ValidateLifetime = true,
                            NameClaimType = "preferred_username",
                            RoleClaimType = "roles"
                        };

                        // Avoid HTML redirect on 401 when using APIs
                        o.Events = new JwtBearerEvents
                        {
                            OnMessageReceived = ctx =>
                            {
                                var accessToken = ctx.Request.Query["access_token"];
                                var path = ctx.HttpContext.Request.Path;
                                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                                {
                                    ctx.Token = accessToken;
                                }

                                return Task.CompletedTask;
                            },
                            OnChallenge = ctx =>
                            {
                                ctx.HandleResponse();
                                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                                return Task.CompletedTask;
                            }
                        };

                        o.RefreshOnIssuerKeyNotFound = true;
                    });

                builder.Services.AddAuthorization();
                builder.Services.AddSignalR().AddJsonProtocol((options) =>
                {
                    options.PayloadSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
                });
                builder.Services.AddHostedService<RealtimeEventsWorker>();

                builder.Services.AddHealthChecks().AddCheck<MicroservicesHealthCheck>("microservices");
                builder.Services.AddGatewayServices();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("cors_policy", p => p
                        .WithOrigins(appSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials());
                });

                if (builder.Environment.IsEnvironment("Docker"))
                {
                    builder.WebHost.UseUrls("http://0.0.0.0:8080", "https://0.0.0.0:8081");
                }

                var app = builder.Build();

                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseForwardedHeaders();
                // Local `dotnet run` binds HTTP :5000 only; HTTPS redirect is for container/prod.
                if (builder.Environment.IsEnvironment("Docker"))
                {
                    app.UseHttpsRedirection();
                }

                app.UseCors("cors_policy");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGatewayEndpoints();
                app.MapHub<MessengerHub>("/hubs/messenger");
                app.MapHub<WowLfgHub>("/hubs/wow-lfg");

                Log.Information($"Started in {builder.Environment.EnvironmentName} environment...");

                await app.RunAsync();
            }
            catch (HostAbortedException ex)
            {
                Log.Fatal(ex, "Aborted.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Terminated unexpectedly.");
            }
            finally
            {
                Log.Information("Stoped ...");
            }
        }
    }
}
