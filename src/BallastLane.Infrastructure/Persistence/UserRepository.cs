using BallastLane.Application.Abstractions;
using BallastLane.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace BallastLane.Infrastructure.Persistence;

public sealed class UserRepository(AppDbContext db) : IUserRepository
{
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        db.Users.FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        db.Users.Add(user);
        return Task.CompletedTask;
    }
}
