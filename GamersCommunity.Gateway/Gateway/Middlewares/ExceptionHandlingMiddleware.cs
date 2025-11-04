using GamersCommunity.Core.Exceptions;
using Serilog;
using System.Net;
using System.Text.Json;

namespace Gateway.Middlewares
{
    /// <summary>
    /// ASP.NET Core middleware that captures unhandled exceptions, logs them,
    /// and returns a normalized JSON error response.
    /// </summary>
    /// <remarks>
    /// In <c>Development</c> or <c>Testing</c> environments, the stack trace is included in the response body
    /// for troubleshooting. In other environments, only minimal information is returned.
    /// </remarks>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="environment">The hosting environment used to adjust error details.</param>
    public class ExceptionHandlingMiddleware(RequestDelegate next, IHostEnvironment environment)
    {
        private readonly RequestDelegate _next = next;
        private readonly IHostEnvironment _environment = environment;

        /// <summary>
        /// Invokes the middleware for the current HTTP request context.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <returns>A task that represents the completion of request processing.</returns>
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

        /// <summary>
        /// Handles an exception by logging it and writing a JSON error response.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="exception">The thrown exception.</param>
        /// <param name="environment">The hosting environment.</param>
        /// <returns>A task that represents the write operation to the response.</returns>
        private static Task HandleExceptionAsync(HttpContext context, Exception exception, IHostEnvironment environment)
        {
            Log.Error(exception, "Trace ID: {TraceId} - An unhandled exception occurred.", context.TraceIdentifier);

            if (context.Response.HasStarted)
            {
                return Task.CompletedTask;
            }

            var response = new ExceptionResult
            {
                Code = "ERROR",
                Message = "An unexpected error occurred.",
                TraceId = context.TraceIdentifier
            };

            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                response.Exception = exception.StackTrace;
            }

            if (exception is AppException appException)
            {
                context.Response.StatusCode = (int)appException.StatusCode;
                response.Message = exception.Message;
                response.Code = appException.Code;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }

            context.Response.ContentType = "application/json";
            context.Response.Headers["Trace-Id"] = context.TraceIdentifier;

            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }
}
