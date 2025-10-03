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

            // If the response has already started, we cannot write our JSON body safely.
            if (context.Response.HasStarted)
            {
                // Best effort: just log and bail out.
                return Task.CompletedTask;
            }

            var response = new ExceptionResult
            {
                TraceId = context.TraceIdentifier
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
                // Optional: provide a generic safe message
                response.Message = "An unexpected error occurred.";
            }

            context.Response.ContentType = "application/json";
            // Optional: surface the trace id as a response header to ease correlation
            context.Response.Headers["Trace-Id"] = context.TraceIdentifier;

            // Keep serializer simple; align with your global options if you have them
            var json = JsonSerializer.Serialize(response);
            return context.Response.WriteAsync(json);
        }
    }

}
