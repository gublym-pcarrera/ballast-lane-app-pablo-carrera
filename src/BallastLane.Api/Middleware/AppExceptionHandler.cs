using BallastLane.Application.Common;
using Microsoft.AspNetCore.Diagnostics;

namespace BallastLane.Api.Middleware;

public sealed class AppExceptionHandler(ILogger<AppExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ValidationException validation:
                await Results.ValidationProblem(validation.Errors).ExecuteAsync(httpContext);
                return true;
            case NotFoundException notFound:
                await Results.Problem(title: notFound.Message, statusCode: StatusCodes.Status404NotFound).ExecuteAsync(httpContext);
                return true;
            case ConflictException conflict:
                await Results.Problem(title: conflict.Message, statusCode: StatusCodes.Status409Conflict).ExecuteAsync(httpContext);
                return true;
            case AuthenticationFailedException authentication:
                await Results.Problem(title: authentication.Message, statusCode: StatusCodes.Status401Unauthorized).ExecuteAsync(httpContext);
                return true;
            default:
                logger.LogError(exception, "Unhandled exception");
                await Results.Problem(title: "An unexpected error occurred.", statusCode: StatusCodes.Status500InternalServerError).ExecuteAsync(httpContext);
                return true;
        }
    }
}
