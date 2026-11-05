namespace BallastLane.Application.Contracts;

public sealed record UserResponse(Guid Id, string Email, string DisplayName);

public sealed record AuthResponse(string AccessToken, DateTime ExpiresAtUtc, UserResponse User);

public sealed record TaskResponse(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateOnly DueDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
