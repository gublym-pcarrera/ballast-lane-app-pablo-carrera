using BallastLane.Application.Abstractions;
using BallastLane.Application.Common;
using BallastLane.Application.Contracts;
using BallastLane.Domain.Users;

namespace BallastLane.Application.Accounts;

public interface IAccountService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class AccountService(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IAccessTokenFactory tokenFactory,
    TimeProvider clock) : IAccountService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var errors = AccountRules.ValidateRegistration(request.Email, request.Password, request.DisplayName);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var email = AccountRules.NormalizeEmail(request.Email!);
        var existing = await users.GetByEmailAsync(email, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var user = User.Create(
            Guid.CreateVersion7(),
            email,
            AccountRules.NormalizeDisplayName(request.DisplayName!),
            passwordHasher.Hash(request.Password!),
            clock.GetUtcNow().UtcDateTime);

        await users.AddAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ToAuth(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var errors = AccountRules.ValidateLogin(request.Email, request.Password);
        if (errors.Count > 0)
        {
            throw new ValidationException(errors);
        }

        var email = AccountRules.NormalizeEmail(request.Email!);
        var user = await users.GetByEmailAsync(email, cancellationToken);
        if (user is null || !passwordHasher.Verify(request.Password!, user.PasswordHash))
        {
            throw new AuthenticationFailedException("Invalid email or password.");
        }

        return ToAuth(user);
    }

    public async Task<UserResponse> GetAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthenticationFailedException("Sign in again.");
        }

        return ToUser(user);
    }

    private AuthResponse ToAuth(User user)
    {
        var accessToken = tokenFactory.Create(user);
        return new AuthResponse(accessToken.Token, accessToken.ExpiresAtUtc, ToUser(user));
    }

    private static UserResponse ToUser(User user) => new(user.Id, user.Email, user.DisplayName);
}
