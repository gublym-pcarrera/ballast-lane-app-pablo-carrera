using System.Net;
using System.Text.Json;

namespace BallastLane.Api.Tests;

[Collection("api")]
public sealed class AuthEndpointTests(ApiFactory factory)
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Health_IsPublic()
    {
        var response = await client.GetAsync("/api/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("ok", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_ReturnsATokenAndTheStoredProfile()
    {
        var email = UniqueEmail();
        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/register", new
        {
            email,
            password = "Secret123",
            displayName = "Test User"
        }));
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.StatusCode == HttpStatusCode.Created, payload);
        var auth = JsonSerializer.Deserialize<AuthBody>(payload, HttpJson.Options);
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal(email, auth.User.Email);
        Assert.DoesNotContain("Secret123", payload, StringComparison.Ordinal);
        Assert.DoesNotContain("passwordHash", payload, StringComparison.OrdinalIgnoreCase);

        var me = await client.SendAsync(HttpJson.Message(HttpMethod.Get, "/api/auth/me", token: auth.AccessToken));
        var profilePayload = await me.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = JsonSerializer.Deserialize<UserBody>(profilePayload, HttpJson.Options);
        Assert.Equal("Test User", profile?.DisplayName);
    }

    [Fact]
    public async Task Register_RejectsADuplicateEmail()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/register", new
        {
            email,
            password = "Secret123",
            displayName = "Again"
        }));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_ReturnsValidationErrors()
    {
        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/register", new
        {
            email = "not-an-email",
            password = "tiny",
            displayName = ""
        }));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("email", out _));
        Assert.True(document.RootElement.GetProperty("errors").TryGetProperty("password", out _));
    }

    [Fact]
    public async Task Login_RejectsAWrongPasswordWithoutRevealingWhichPartFailed()
    {
        var email = UniqueEmail();
        await RegisterAsync(email);

        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/login", new
        {
            email,
            password = "Secret124"
        }));
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("Invalid email or password.", document.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Me_RequiresAToken()
    {
        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task RegisterAsync(string email)
    {
        var response = await client.SendAsync(HttpJson.Message(HttpMethod.Post, "/api/auth/register", new
        {
            email,
            password = "Secret123",
            displayName = "Test User"
        }));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";
}
