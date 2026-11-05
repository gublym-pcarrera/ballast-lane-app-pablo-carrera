namespace BallastLane.Application.Abstractions;

public sealed record AccessToken(string Token, DateTime ExpiresAtUtc);
