using BallastLane.Application.Abstractions;
using BallastLane.Application.Common;
using BallastLane.Domain.Tasks;
using BallastLane.Domain.Users;
using BallastLane.Infrastructure.Authentication;
using BallastLane.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BallastLane.Infrastructure.Tests;

public sealed class PersistenceTests
{
    [Fact]
    public void PasswordHasher_VerifiesTheOriginalPasswordOnly()
    {
        var hasher = new AspNetPasswordHasher();
        var hash = hasher.Hash("Secret123");

        Assert.NotEqual("Secret123", hash);
        Assert.True(hasher.Verify("Secret123", hash));
        Assert.False(hasher.Verify("Secret124", hash));
    }

    [Fact]
    public async Task Tasks_AreListedByOwnerAndDueDate()
    {
        await using var database = new TestDatabase();
        var owner = await AddUserAsync(database, "owner@example.com");
        var other = await AddUserAsync(database, "other@example.com");
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        database.Context.Tasks.AddRange(
            TaskItem.Create(Guid.NewGuid(), owner, "Later", "", TaskState.Todo, new DateOnly(2026, 6, 20), now),
            TaskItem.Create(Guid.NewGuid(), owner, "Alpha", "", TaskState.Todo, new DateOnly(2026, 6, 16), now),
            TaskItem.Create(Guid.NewGuid(), owner, "Beta", "", TaskState.Todo, new DateOnly(2026, 6, 16), now),
            TaskItem.Create(Guid.NewGuid(), other, "Hidden", "", TaskState.Todo, new DateOnly(2026, 6, 16), now));
        await database.Context.SaveChangesAsync();

        var repository = new TaskRepository(database.Context);
        var titles = (await repository.ListByOwnerAsync(owner, TaskState.Todo, CancellationToken.None)).Select(task => task.Title).ToArray();

        Assert.Equal(["Alpha", "Beta", "Later"], titles);
    }

    [Fact]
    public async Task DuplicateEmail_IsRejectedByTheUnitOfWork()
    {
        await using var database = new TestDatabase();
        var users = new UserRepository(database.Context);
        var unitOfWork = new UnitOfWork(database.Context);
        var now = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);
        await users.AddAsync(User.Create(Guid.NewGuid(), "ada@example.com", "Ada", "hash", now), CancellationToken.None);
        await unitOfWork.SaveChangesAsync(CancellationToken.None);
        await users.AddAsync(User.Create(Guid.NewGuid(), "ada@example.com", "Ada", "hash", now), CancellationToken.None);

        var exception = await Assert.ThrowsAsync<ConflictException>(() => unitOfWork.SaveChangesAsync(CancellationToken.None));

        Assert.Equal("An account with this email already exists.", exception.Message);
    }

    [Fact]
    public void Seed_CreatesTheDemoUserOnce()
    {
        using var database = new TestDatabase();
        var hasher = new AspNetPasswordHasher();
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

        DbSeeder.Seed(database.Context, hasher, clock);
        DbSeeder.Seed(database.Context, hasher, clock);

        var user = Assert.Single(database.Context.Users);
        Assert.Equal(DbSeeder.DemoEmail, user.Email);
        Assert.Equal(DbSeeder.DemoDisplayName, user.DisplayName);
        Assert.True(hasher.Verify(DbSeeder.DemoPassword, user.PasswordHash));
        Assert.Equal(3, database.Context.Tasks.Count());
        Assert.All(database.Context.Tasks, task => Assert.Equal(user.Id, task.OwnerId));
    }

    [Fact]
    public void Seed_MovesAnExistingDemoUserOntoTheCurrentEmail()
    {
        using var database = new TestDatabase();
        var hasher = new AspNetPasswordHasher();
        var clock = new FixedClock(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));
        var now = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        database.Context.Users.Add(User.Create(DbSeeder.DemoUserId, "demo@ballast.local", DbSeeder.DemoDisplayName, hasher.Hash("Oldpass1"), now));
        database.Context.SaveChanges();

        DbSeeder.Seed(database.Context, hasher, clock);

        var user = Assert.Single(database.Context.Users);
        Assert.Equal(DbSeeder.DemoEmail, user.Email);
        Assert.True(hasher.Verify(DbSeeder.DemoPassword, user.PasswordHash));
        Assert.Empty(database.Context.Tasks);
    }

    private static async Task<Guid> AddUserAsync(TestDatabase database, string email)
    {
        var user = User.Create(Guid.NewGuid(), email, "User", "hash", DateTime.UnixEpoch);
        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        return user.Id;
    }

    private sealed class FixedClock(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}

internal sealed class TestDatabase : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public TestDatabase()
    {
        connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlite(connection).Options;
        Context = new AppDbContext(options);
        Context.Database.Migrate();
    }

    public AppDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        await Context.DisposeAsync();
        await connection.DisposeAsync();
    }
}
