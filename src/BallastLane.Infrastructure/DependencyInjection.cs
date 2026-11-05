using System.Text;
using BallastLane.Application.Abstractions;
using BallastLane.Infrastructure.Authentication;
using BallastLane.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string databasePath)
    {
        var signingKey = configuration["Jwt:SigningKey"];
        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
        }

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddDbContext<AppDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();
        services.AddSingleton<IAccessTokenFactory, JwtAccessTokenFactory>();
        services.AddSingleton(TimeProvider.System);
        return services;
    }
}
