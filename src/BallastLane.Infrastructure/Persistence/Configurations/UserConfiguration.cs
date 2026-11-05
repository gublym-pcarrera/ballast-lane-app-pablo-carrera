using BallastLane.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BallastLane.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Email).HasMaxLength(256).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
        builder.Property(user => user.DisplayName).HasMaxLength(80).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(user => user.CreatedAtUtc).IsRequired();
    }
}
