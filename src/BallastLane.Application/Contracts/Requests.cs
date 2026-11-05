namespace BallastLane.Application.Contracts;

public sealed record RegisterRequest(string? Email, string? Password, string? DisplayName);

public sealed record LoginRequest(string? Email, string? Password);

public sealed record UpsertTaskRequest(string? Title, string? Description, string? Status, DateOnly? DueDate);
