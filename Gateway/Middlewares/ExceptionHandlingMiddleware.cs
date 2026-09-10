using GamersCommunity.Core.Exceptions;
using Gateway.Abstractions;
using Gateway.Extensions;
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
    /// Expected <see cref="AppException"/> failures are logged as a single message line without a stack trace:
    /// they are controlled errors, and microservice <see cref="RpcException"/> stacks were already recorded by
    /// the consumer. Only unexpected exceptions dump their stack, both in the log and — under
    /// <c>Development</c> or <c>Testing</c> — in the HTTP body.
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
            if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
            {
                Log.Debug(
                    "Trace ID: {TraceId} - Request aborted by the client.",
                    context.TraceIdentifier);
                return Task.CompletedTask;
            }

            LogException(context, exception);

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

            if (exception is not AppException && (environment.IsDevelopment() || environment.IsEnvironment("Testing")))
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

        /// <summary>
        /// Writes a single error log entry, with a stack trace only for unexpected exceptions.
        /// </summary>
        /// <param name="context">The current HTTP context.</param>
        /// <param name="exception">The thrown exception.</param>
        private static void LogException(HttpContext context, Exception exception)
        {
            if (exception is not AppException appException)
            {
                Log.Error(exception, "Trace ID: {TraceId} - An unhandled exception occurred.", context.TraceIdentifier);
                return;
            }

            var request = GatewayRequestLogContext.From(context);

            if (appException is RpcException)
            {
                var queue = request.Microservice is { Length: > 0 } ms
                    ? context.RequestServices.GetService<IGatewayRouter>()?.ResolveQueue(ms)
                    : null;

                Log.Error(
                    "Trace ID: {TraceId} - RpcException on {Method} {Path} (ms={Microservice}, queue={Queue}, resource={Resource}, action={Action}, id={Id}, publicId={PublicId}): [{Code}] {Message}",
                    context.TraceIdentifier,
                    request.Method,
                    request.Path,
                    request.Microservice ?? "-",
                    queue ?? "-",
                    request.Resource ?? "-",
                    request.Action ?? "-",
                    request.Id ?? "-",
                    request.PublicId ?? "-",
                    appException.Code,
                    appException.Message);
                return;
            }

            Log.Error(
                "Trace ID: {TraceId} - {ExceptionType} on {Method} {Path}: [{Code}] {Message}",
                context.TraceIdentifier,
                exception.GetType().Name,
                request.Method,
                request.Path,
                appException.Code,
                appException.Message);
        }
    }
}
