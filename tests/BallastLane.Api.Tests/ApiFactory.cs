using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BallastLane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BallastLane.Api.Tests;

[CollectionDefinition("api")]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");

    public async Task InitializeAsync()
    {
        await connection.OpenAsync();
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await connection.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(service =>
                service.ServiceType == typeof(DbContextOptions<AppDbContext>)
                || service.ServiceType == typeof(AppDbContext)).ToList();
            foreach (var descriptor in descriptors)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }
}

internal static class HttpJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static HttpRequestMessage Message(HttpMethod method, string url, object? body = null, string? token = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body, options: Options);
        }

        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return request;
    }
}

internal sealed record AuthBody(string AccessToken, UserBody User);

internal sealed record UserBody(Guid Id, string Email, string DisplayName);

internal sealed record TaskBody(
    Guid Id,
    string Title,
    string Description,
    string Status,
    DateOnly DueDate,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
