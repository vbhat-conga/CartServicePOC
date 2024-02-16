using CartServicePOC.Model;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace CartServicePOC.Extensions
{
    public static class ExceptionMiddlewareExtensions
    {
        public static void ConfigureExceptionHandler(this IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Program>();
            _ = app.UseExceptionHandler(appError =>
            {
                appError.Run(async context =>
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.ContentType = "application/json";
                    var contextFeature = context.Features.Get<IExceptionHandlerFeature>();
                    if (contextFeature != null)
                    {
                        logger.LogError($"Something went wrong: {contextFeature.Error}");
                        var problemDetail = new ProblemDetails
                        {
                            Title = "Internal server error",
                            Detail = contextFeature.Error.Message,
                            Instance = context.Request.Path,
                            Status = context.Response.StatusCode,
                            Type = "https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1"
                        };
                        await context.Response.WriteAsync(new ApiResponse<ProblemDetails>(problemDetail, 500).ToString()!);
                    }
                });
            });
        }
    }
}
