using BallastLane.Domain.Tasks;
using BallastLane.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BallastLane.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Title).HasMaxLength(120).IsRequired();
        builder.Property(task => task.Description).HasMaxLength(2000).IsRequired();
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(task => task.DueDate).IsRequired();
        builder.Property(task => task.CreatedAtUtc).IsRequired();
        builder.Property(task => task.UpdatedAtUtc).IsRequired();
        builder.HasIndex(task => task.OwnerId);
        builder.HasOne<User>().WithMany().HasForeignKey(task => task.OwnerId).OnDelete(DeleteBehavior.Cascade);
    }
}
