using BallastLane.Application.Abstractions;
using BallastLane.Domain.Tasks;
using BallastLane.Domain.Users;

namespace BallastLane.Infrastructure.Persistence;

public static class DbSeeder
{
    public const string DemoEmail = "demo@ballastlane.com";
    public const string DemoPassword = "Demo1234!";
    public const string DemoDisplayName = "Demo User";
    public static readonly Guid DemoUserId = Guid.Parse("8d0c7e3a-1b5a-4c1e-9f2a-6b0d1e4a7c21");

    public static void Seed(AppDbContext db, IPasswordHasher passwordHasher, TimeProvider clock)
    {
        var existing = db.Users.SingleOrDefault(user => user.Id == DemoUserId);
        if (existing is not null)
        {
            if (existing.Email != DemoEmail)
            {
                db.Entry(existing).Property(user => user.Email).CurrentValue = DemoEmail;
                db.Entry(existing).Property(user => user.PasswordHash).CurrentValue = passwordHasher.Hash(DemoPassword);
                db.SaveChanges();
            }

            return;
        }

        if (db.Users.Any(user => user.Email == DemoEmail))
        {
            return;
        }

        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var user = User.Create(DemoUserId, DemoEmail, DemoDisplayName, passwordHasher.Hash(DemoPassword), now);
        db.Users.Add(user);
        db.Tasks.AddRange(
            TaskItem.Create(
                Guid.Parse("11111111-1111-4111-8111-111111111111"),
                user.Id,
                "Outline the architecture walkthrough",
                "Explain the four projects and why business rules do not reference the database.",
                TaskState.InProgress,
                today.AddDays(1),
                now),
            TaskItem.Create(
                Guid.Parse("22222222-2222-4222-8222-222222222222"),
                user.Id,
                "Review the list on a phone",
                "Check that filters, create, edit, and delete still fit a narrow screen.",
                TaskState.Todo,
                today.AddDays(4),
                now),
            TaskItem.Create(
                Guid.Parse("33333333-3333-4333-8333-333333333333"),
                user.Id,
                "Confirm the demo account can sign in",
                "Use the seeded credentials and open your own tasks.",
                TaskState.Done,
                today,
                now));
        db.SaveChanges();
    }
}
