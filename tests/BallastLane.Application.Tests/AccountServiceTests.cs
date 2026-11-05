using BallastLane.Application.Abstractions;
using BallastLane.Application.Accounts;
using BallastLane.Application.Common;
using BallastLane.Application.Contracts;
using BallastLane.Domain.Users;

namespace BallastLane.Application.Tests;

public sealed class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Register_StoresNormalizedEmailAndDoesNotKeepTheRawPassword()
    {
        var users = new InMemoryUsers();
        var service = CreateService(users);

        var response = await service.RegisterAsync(
            new RegisterRequest(" Ada@Example.com ", "Secret123", "  Ada  "),
            CancellationToken.None);

        var saved = Assert.Single(users.Users);
        Assert.Equal("ada@example.com", saved.Email);
        Assert.Equal("Ada", saved.DisplayName);
        Assert.NotEqual("Secret123", saved.PasswordHash);
        Assert.Equal("ada@example.com", response.User.Email);
        Assert.Equal("token-" + saved.Id, response.AccessToken);
    }

    [Fact]
    public async Task Register_RejectsDuplicateEmailIgnoringCase()
    {
        var service = CreateService(new InMemoryUsers());
        await service.RegisterAsync(new RegisterRequest("ada@example.com", "Secret123", "Ada"), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() =>
            service.RegisterAsync(new RegisterRequest("Ada@Example.com", "Secret123", "Ada Again"), CancellationToken.None));

        Assert.Equal("An account with this email already exists.", exception.Message);
    }

    [Theory]
    [InlineData("", "Secret123", "Ada", "email")]
    [InlineData("ada@example.com", "short1", "Ada", "password")]
    [InlineData("ada@example.com", "longenough", "Ada", "password")]
    [InlineData("ada@example.com", "12345678", "Ada", "password")]
    [InlineData("ada@example.com", "Secret123", "A", "displayName")]
    public async Task Register_ReturnsFieldErrors(string email, string password, string displayName, string field)
    {
        var service = CreateService(new InMemoryUsers());

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            service.RegisterAsync(new RegisterRequest(email, password, displayName), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(field));
    }

    [Fact]
    public async Task Login_AcceptsTheNormalizedEmail()
    {
        var service = CreateService(new InMemoryUsers());
        await service.RegisterAsync(new RegisterRequest("Ada@Example.com", "Secret123", "Ada"), CancellationToken.None);

        var response = await service.LoginAsync(new LoginRequest("ada@example.com", "Secret123"), CancellationToken.None);

        Assert.Equal("ada@example.com", response.User.Email);
    }

    [Fact]
    public async Task Login_UsesTheSameMessageForUnknownEmailAndWrongPassword()
    {
        var service = CreateService(new InMemoryUsers());
        await service.RegisterAsync(new RegisterRequest("ada@example.com", "Secret123", "Ada"), CancellationToken.None);

        var unknown = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest("missing@example.com", "Secret123"), CancellationToken.None));
        var wrong = await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            service.LoginAsync(new LoginRequest("ada@example.com", "Secret124"), CancellationToken.None));

        Assert.Equal("Invalid email or password.", unknown.Message);
        Assert.Equal(unknown.Message, wrong.Message);
    }

    [Fact]
    public async Task Get_ReturnsTheStoredProfile()
    {
        var service = CreateService(new InMemoryUsers());
        var registered = await service.RegisterAsync(new RegisterRequest("ada@example.com", "Secret123", "Ada"), CancellationToken.None);

        var profile = await service.GetAsync(registered.User.Id, CancellationToken.None);

        Assert.Equal("Ada", profile.DisplayName);
        Assert.Equal("ada@example.com", profile.Email);
    }

    private static AccountService CreateService(InMemoryUsers users) =>
        new(users, new NoopUnitOfWork(), new PlainPasswordHasher(), new FakeTokens(), new FixedTimeProvider(Now));
}

internal sealed class PlainPasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

internal sealed class FakeTokens : IAccessTokenFactory
{
    public AccessToken Create(User user) => new("token-" + user.Id, new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}
