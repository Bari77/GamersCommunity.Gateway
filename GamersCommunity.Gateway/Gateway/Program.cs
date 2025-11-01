using GamersCommunity.Core.Exceptions;
using GamersCommunity.Core.Logging;
using GamersCommunity.Core.Rabbit;
using Gateway.Configuration;
using Gateway.Endpoints;
using Gateway.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
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

                #region Logger

                var loggerSettingsSection = builder.Configuration.GetSection("LoggerSettings");
                var loggerSettings = loggerSettingsSection.Get<LoggerSettings>() ?? throw new InternalServerErrorException("Can't parse LoggerSettings section");
                builder.Services.Configure<LoggerSettings>(loggerSettingsSection.Bind);

                #endregion

                #region RabbitMQ

                builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ").Bind);

                #endregion

                #region AppSettings

                var appSettingsSection = builder.Configuration.GetSection("AppSettings");
                var appSettings = appSettingsSection.Get<AppSettings>() ?? throw new InternalServerErrorException("Can't parse AppSettings section");
                builder.Services.Configure<AppSettings>(appSettingsSection.Bind);

                #endregion

                #region Gateway routing

                builder.Services.Configure<GatewayRoutingSettings>(builder.Configuration.GetSection("GatewayRouting").Bind);

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
                        var keycloak = appSettings.Keycloak;

                        // Base Keycloak info
                        o.Authority = keycloak.Authority;
                        o.Audience = keycloak.Audience;
                        o.MetadataAddress = $"{keycloak.Authority}/.well-known/openid-configuration";
                        o.RequireHttpsMetadata = keycloak.RequireHttpsMetadata;

                        // Token validation
                        o.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = true,
                            ValidIssuer = keycloak.Authority,

                            ValidateAudience = true,
                            ValidAudiences = ["account", "gc-front", "gc-gateway-api"],

                            ValidateLifetime = true,
                            NameClaimType = "preferred_username",
                            RoleClaimType = "roles"
                        };

                        // Avoid HTML redirect on 401 when using APIs
                        o.Events = new JwtBearerEvents
                        {
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

                builder.Services.AddGatewayServices();

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("cors_policy", p => p
                        .WithOrigins(appSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
                });

                builder.WebHost.UseUrls("http://0.0.0.0:8080", "https://0.0.0.0:8081");

                var app = builder.Build();

                app.UseMiddleware<ExceptionHandlingMiddleware>();

                app.UseForwardedHeaders();
                app.UseHttpsRedirection();

                app.UseCors("cors_policy");

                app.UseAuthentication();
                app.UseAuthorization();

                app.MapGatewayEndpoints();

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