using BallastLane.Application.Accounts;
using BallastLane.Application.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IAccountService, AccountService>();
        return services;
    }
}
