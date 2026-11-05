using System.Net;
using System.Text.Json;

namespace BallastLane.Api.Tests;

[Collection("api")]
public sealed class TaskEndpointTests(ApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Tasks_RequireAToken()
    {
        var response = await client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Owner_CanCreateUpdateListAndDeleteOnlyTheirTasks()
    {
        var ownerToken = await RegisterAsync();
        var otherToken = await RegisterAsync();

        var created = await SendAsync(HttpMethod.Post, "/api/tasks", ownerToken, new
        {
            title = "  Prepare notes ",
            description = "Cover ownership",
            status = "Todo",
            dueDate = "2026-12-01"
        });
        var createdPayload = await created.Content.ReadAsStringAsync();
        Assert.True(created.StatusCode == HttpStatusCode.Created, createdPayload);
        var task = JsonSerializer.Deserialize<TaskBody>(createdPayload, HttpJson.Options);
        Assert.NotNull(task);
        Assert.Equal("Prepare notes", task.Title);
        Assert.Equal("Todo", task.Status);

        var hidden = await SendAsync(HttpMethod.Get, $"/api/tasks/{task.Id}", otherToken);
        Assert.Equal(HttpStatusCode.NotFound, hidden.StatusCode);

        var updated = await SendAsync(HttpMethod.Put, $"/api/tasks/{task.Id}", ownerToken, new
        {
            title = "Prepare notes",
            description = "Updated",
            status = "InProgress",
            dueDate = "2026-12-02"
        });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);

        var list = await SendAsync(HttpMethod.Get, "/api/tasks?status=InProgress", ownerToken);
        var items = JsonSerializer.Deserialize<List<TaskBody>>(await list.Content.ReadAsStringAsync(), HttpJson.Options);
        Assert.NotNull(items);
        Assert.Contains(items, item => item.Id == task.Id && item.Status == "InProgress");

        var otherList = await SendAsync(HttpMethod.Get, "/api/tasks", otherToken);
        var otherItems = JsonSerializer.Deserialize<List<TaskBody>>(await otherList.Content.ReadAsStringAsync(), HttpJson.Options);
        Assert.NotNull(otherItems);
        Assert.DoesNotContain(otherItems, item => item.Id == task.Id);

        var deleted = await SendAsync(HttpMethod.Delete, $"/api/tasks/{task.Id}", ownerToken);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);

        var missing = await SendAsync(HttpMethod.Get, $"/api/tasks/{task.Id}", ownerToken);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task Create_ReturnsValidationErrorsForABadStatus()
    {
        var token = await RegisterAsync();
        var response = await SendAsync(HttpMethod.Post, "/api/tasks", token, new
        {
            title = "Bad",
            description = "",
            status = "Later",
            dueDate = "2026-12-01"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<string> RegisterAsync()
    {
        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/register", new
        {
            email = $"user-{Guid.NewGuid():N}@example.com",
            password = "Secret123",
            displayName = "Test User"
        }));
        var payload = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == HttpStatusCode.Created, payload);
        var auth = JsonSerializer.Deserialize<AuthBody>(payload, HttpJson.Options);
        return auth!.AccessToken;
    }

    private Task<HttpResponseMessage> SendAsync(HttpMethod method, string url, string token, object? body = null) =>
        client.SendAsync(HttpJson.Message(method, url, body, token));
}
