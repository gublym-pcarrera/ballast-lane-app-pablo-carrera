using BallastLane.Application.Abstractions;
using BallastLane.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace BallastLane.Infrastructure.Authentication;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> hasher = new();

    public string Hash(string password) => hasher.HashPassword(user: null!, password);

    public bool Verify(string password, string passwordHash)
    {
        var result = hasher.VerifyHashedPassword(user: null!, hashedPassword: passwordHash, providedPassword: password);
        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
