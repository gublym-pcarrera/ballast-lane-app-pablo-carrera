namespace BallastLane.Domain.Tasks;

public sealed class TaskItem
{
    private TaskItem()
    {
    }

    public Guid Id { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public TaskState Status { get; private set; }

    public DateOnly DueDate { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime UpdatedAtUtc { get; private set; }

    public static TaskItem Create(
        Guid id,
        Guid ownerId,
        string title,
        string description,
        TaskState status,
        DateOnly dueDate,
        DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id is required.", nameof(id));
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Owner id is required.", nameof(ownerId));
        }

        return new TaskItem
        {
            Id = id,
            OwnerId = ownerId,
            Title = title,
            Description = description,
            Status = status,
            DueDate = dueDate,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };
    }

    public void Update(string title, string description, TaskState status, DateOnly dueDate, DateTime updatedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(description);
        Title = title;
        Description = description;
        Status = status;
        DueDate = dueDate;
        UpdatedAtUtc = updatedAtUtc;
    }
}
