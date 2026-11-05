using BallastLane.Domain.Tasks;

namespace BallastLane.Application.Tasks;

public static class TaskRules
{
    public const int TitleMaxLength = 120;
    public const int DescriptionMaxLength = 2000;
    public const string StatusMessage = "Status must be Todo, InProgress, or Done.";
    public const string NewTaskStatusMessage = "New tasks start as Todo.";
    public const string PastDueDateMessage = "Due date cannot be before today.";

    public static Dictionary<string, string[]> Validate(
        string? title,
        string? description,
        string? status,
        DateOnly? dueDate,
        DateOnly today,
        bool newTask)
    {
        var errors = new Dictionary<string, string[]>();
        var trimmedTitle = title?.Trim() ?? string.Empty;
        if (trimmedTitle.Length == 0)
        {
            errors["title"] = ["Title is required."];
        }
        else if (trimmedTitle.Length > TitleMaxLength)
        {
            errors["title"] = [$"Title must be {TitleMaxLength} characters or fewer."];
        }

        if ((description?.Trim().Length ?? 0) > DescriptionMaxLength)
        {
            errors["description"] = [$"Description must be {DescriptionMaxLength} characters or fewer."];
        }

        if (!TryParseStatus(status, out var parsedStatus))
        {
            errors["status"] = [StatusMessage];
        }
        else if (newTask && parsedStatus != TaskState.Todo)
        {
            errors["status"] = [NewTaskStatusMessage];
        }

        if (dueDate is null)
        {
            errors["dueDate"] = ["Due date is required."];
        }
        else if (dueDate.Value < today)
        {
            errors["dueDate"] = [PastDueDateMessage];
        }

        return errors;
    }

    public static string NormalizeTitle(string title) => title.Trim();

    public static string NormalizeDescription(string? description) => description?.Trim() ?? string.Empty;

    public static bool TryParseStatus(string? status, out TaskState parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(status))
        {
            return false;
        }

        var names = Enum.GetNames<TaskState>();
        if (!names.Contains(status, StringComparer.Ordinal))
        {
            return false;
        }

        return Enum.TryParse(status, out parsed);
    }

    public static TaskState ParseStatus(string status)
    {
        if (!TryParseStatus(status, out var parsed))
        {
            throw new ArgumentException(StatusMessage, nameof(status));
        }

        return parsed;
    }
}
