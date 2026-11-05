using BallastLane.Application.Abstractions;
using BallastLane.Application.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BallastLane.Infrastructure.Persistence;

public sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            throw new ConflictException("An account with this email already exists.");
        }
    }

    private static bool IsUniqueViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqlite
                && sqlite.SqliteErrorCode == 19
                && sqlite.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
