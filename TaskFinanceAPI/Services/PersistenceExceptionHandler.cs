using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace TaskFinanceAPI.Services;

public sealed class PersistenceExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var title = exception switch
        {
            DbUpdateConcurrencyException => "The resource changed during this request.",
            DbUpdateException { InnerException: SqlException { Number: 547 } } =>
                "The operation conflicts with a database constraint.",
            _ => null
        };

        if (title is null)
        {
            return false;
        }

        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails { Title = title, Status = context.Response.StatusCode }
        });
        return true;
    }
}
