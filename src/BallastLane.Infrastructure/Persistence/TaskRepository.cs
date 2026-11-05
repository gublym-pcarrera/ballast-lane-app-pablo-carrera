using BallastLane.Application.Abstractions;
using BallastLane.Domain.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BallastLane.Infrastructure.Persistence;

public sealed class TaskRepository(AppDbContext db) : ITaskRepository
{
    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid ownerId, TaskState? status, CancellationToken cancellationToken)
    {
        var query = db.Tasks.Where(task => task.OwnerId == ownerId);
        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        return await query.OrderBy(task => task.DueDate).ThenBy(task => task.Title).ToListAsync(cancellationToken);
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        db.Tasks.Add(task);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        db.Tasks.Remove(task);
        return Task.CompletedTask;
    }
}
