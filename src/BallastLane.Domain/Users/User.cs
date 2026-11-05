namespace BallastLane.Domain.Users;

public sealed class User
{
    private User()
    {
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string PasswordHash { get; private set; } = string.Empty;

    public DateTime CreatedAtUtc { get; private set; }

    public static User Create(Guid id, string email, string displayName, string passwordHash, DateTime createdAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        if (id == Guid.Empty)
        {
            throw new ArgumentException("User id is required.", nameof(id));
        }

        return new User
        {
            Id = id,
            Email = email,
            DisplayName = displayName,
            PasswordHash = passwordHash,
            CreatedAtUtc = createdAtUtc
        };
    }
}
