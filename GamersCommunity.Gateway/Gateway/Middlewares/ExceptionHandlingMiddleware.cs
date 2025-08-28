using GamersCommunity.Core.Exceptions;
using Serilog;
using System.Net;
using System.Text.Json;

namespace Gateway.Middlewares
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        private readonly RequestDelegate _next = next;
        private readonly IHostEnvironment _environment = environment;

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, _environment);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment environment)
        {
            Log.Error(exception, $"Trace ID: {context.TraceIdentifier} - An unhandled exception occurred.");

            var response = new ExceptionResult
            {
                TraceId = context.TraceIdentifier,
            };

            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                response.Exception = exception.StackTrace;
            }

            if (exception is IAppException appException)
            {
                context.Response.StatusCode = (int)appException.Code;
                response.Message = exception.Message;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            context.Response.ContentType = "application/json";

            return context.Response.WriteAsync(JsonSerializer.Serialize(response));
        }
    }

}
