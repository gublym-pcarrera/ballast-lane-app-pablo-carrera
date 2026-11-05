using BallastLane.Api.Controllers;
using BallastLane.Application;
using BallastLane.Domain.Users;
using BallastLane.Infrastructure.Persistence;
using NetArchTest.Rules;

namespace BallastLane.Architecture.Tests;

public sealed class LayerTests
{
    [Fact]
    public void Domain_DoesNotDependOnOuterLayers()
    {
        var result = Types.InAssembly(typeof(User).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("BallastLane.Application", "BallastLane.Infrastructure", "BallastLane.Api", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Application_DoesNotDependOnInfrastructureOrApi()
    {
        var result = Types.InAssembly(typeof(DependencyInjection).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("BallastLane.Infrastructure", "BallastLane.Api", "Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnTheApi()
    {
        var result = Types.InAssembly(typeof(AppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOn("BallastLane.Api")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }

    [Fact]
    public void Controllers_DoNotDependOnTheDatabase()
    {
        var result = Types.InAssembly(typeof(TasksController).Assembly)
            .That()
            .ResideInNamespace("BallastLane.Api.Controllers")
            .ShouldNot()
            .HaveDependencyOn("BallastLane.Infrastructure")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
