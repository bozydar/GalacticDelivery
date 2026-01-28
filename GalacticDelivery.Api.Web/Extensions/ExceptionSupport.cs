using Microsoft.AspNetCore.Diagnostics;

namespace GalacticDelivery.Api.Web.Extensions;

public static class ExceptionSupport
{
    public static void SetExceptionSupport(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(correlationId))
            {
                correlationId = context.TraceIdentifier;
            }

            context.Items["CorrelationId"] = correlationId;
            context.Response.OnStarting(() =>
            {
                context.Response.Headers["X-Correlation-ID"] = correlationId;
                return Task.CompletedTask;
            });

            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            using (logger.BeginScope(new Dictionary<string, object?> { ["CorrelationId"] = correlationId }))
            {
                await next();
            }
        });

        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var exceptionHandler = context.Features.Get<IExceptionHandlerFeature>();
                var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                var correlationId = context.Items["CorrelationId"] as string
                                    ?? context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                                    ?? context.TraceIdentifier;

                if (exceptionHandler?.Error is not null)
                {
                    logger.LogError(exceptionHandler.Error, "Unhandled exception for {Method} {Path} ({CorrelationId})",
                        context.Request.Method, context.Request.Path, correlationId);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new { error = "internal_error", correlationId });
            });
        });
    }
}
