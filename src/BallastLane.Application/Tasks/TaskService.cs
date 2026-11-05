using BallastLane.Application.Abstractions;
using BallastLane.Application.Common;
using BallastLane.Application.Contracts;
using BallastLane.Domain.Tasks;

namespace BallastLane.Application.Tasks;

public interface ITaskService
{
    Task<IReadOnlyList<TaskResponse>> ListAsync(Guid ownerId, string? status, CancellationToken cancellationToken);

    Task<TaskResponse> GetAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);

    Task<TaskResponse> CreateAsync(Guid ownerId, UpsertTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse> UpdateAsync(Guid ownerId, Guid taskId, UpsertTaskRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken);
}

public sealed class TaskService(ITaskRepository tasks, IUnitOfWork unitOfWork, TimeProvider clock) : ITaskService
{
    public async Task<IReadOnlyList<TaskResponse>> ListAsync(Guid ownerId, string? status, CancellationToken cancellationToken)
    {
        TaskState? parsed = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!TaskRules.TryParseStatus(status, out var value))
            {
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["status"] = [TaskRules.StatusMessage]
                });
            }

            parsed = value;
        }

        var items = await tasks.ListByOwnerAsync(ownerId, parsed, cancellationToken);
        return items.Select(ToResponse).ToList();
    }

    public async Task<TaskResponse> GetAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await FindOwnedAsync(ownerId, taskId, cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse> CreateAsync(Guid ownerId, UpsertTaskRequest request, CancellationToken cancellationToken)
    {
        var errors = TaskRules.Validate(request.Title, request.Description, request.Status, request.DueDate, Today(), newTask: true);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var task = TaskItem.Create(
            Guid.CreateVersion7(),
            ownerId,
            TaskRules.NormalizeTitle(request.Title!),
            TaskRules.NormalizeDescription(request.Description),
            TaskRules.ParseStatus(request.Status!),
            request.DueDate!.Value,
            now);

        await tasks.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task<TaskResponse> UpdateAsync(Guid ownerId, Guid taskId, UpsertTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await FindOwnedAsync(ownerId, taskId, cancellationToken);
        var errors = TaskRules.Validate(request.Title, request.Description, request.Status, request.DueDate, Today(), newTask: false, task.DueDate);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        task.Update(
            TaskRules.NormalizeTitle(request.Title!),
            TaskRules.NormalizeDescription(request.Description),
            TaskRules.ParseStatus(request.Status!),
            request.DueDate!.Value,
            clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToResponse(task);
    }

    public async Task DeleteAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await FindOwnedAsync(ownerId, taskId, cancellationToken);
        await tasks.DeleteAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private DateOnly Today()
    {
        var local = TimeZoneInfo.ConvertTime(clock.GetUtcNow(), TimeZoneInfo.Local);
        return DateOnly.FromDateTime(local.DateTime);
    }

    private async Task<TaskItem> FindOwnedAsync(Guid ownerId, Guid taskId, CancellationToken cancellationToken)
    {
        var task = await tasks.GetByIdAsync(taskId, cancellationToken);
        if (task is null || task.OwnerId != ownerId)
        {
            throw new NotFoundException("Task was not found.");
        }

        return task;
    }

    private static TaskResponse ToResponse(TaskItem task) =>
        new(task.Id, task.Title, task.Description, task.Status.ToString(), task.DueDate, task.CreatedAtUtc, task.UpdatedAtUtc);
}
