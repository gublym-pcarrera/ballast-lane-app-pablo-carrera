using BallastLane.Domain.Tasks;

namespace BallastLane.Application.Abstractions;

public interface ITaskRepository
{
    Task<TaskItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<TaskItem>> ListByOwnerAsync(Guid ownerId, TaskState? status, CancellationToken cancellationToken);

    Task AddAsync(TaskItem task, CancellationToken cancellationToken);

    Task DeleteAsync(TaskItem task, CancellationToken cancellationToken);
}
