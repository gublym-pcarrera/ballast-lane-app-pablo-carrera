using BallastLane.Application.Abstractions;
using BallastLane.Domain.Tasks;
using BallastLane.Domain.Users;

namespace BallastLane.Application.Tests;

internal sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => utcNow;
}

internal sealed class NoopUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class InMemoryUsers : IUserRepository
{
    public List<User> Users { get; } = [];

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        Task.FromResult(Users.FirstOrDefault(user => user.Email == email));

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Users.Add(user);
        return Task.CompletedTask;
    }
}

internal sealed class InMemoryTasks : ITaskRepository
{
    public List<TaskItem> Items { get; } = [];

    public Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(Items.FirstOrDefault(task => task.Id == id));

    public Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid ownerId, TaskState? status, CancellationToken cancellationToken)
    {
        IEnumerable<TaskItem> query = Items.Where(task => task.OwnerId == ownerId);
        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        IReadOnlyList<TaskItem> results = query.OrderBy(task => task.DueDate).ThenBy(task => task.Title).ToList();
        return Task.FromResult(results);
    }

    public Task AddAsync(TaskItem task, CancellationToken cancellationToken)
    {
        Items.Add(task);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(TaskItem task, CancellationToken cancellationToken)
    {
        Items.Remove(task);
        return Task.CompletedTask;
    }
}
