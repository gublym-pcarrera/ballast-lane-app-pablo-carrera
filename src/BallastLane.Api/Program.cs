using System.Text;
using BallastLane.Api.Middleware;
using BallastLane.Api.Security;
using BallastLane.Application;
using BallastLane.Infrastructure;
using BallastLane.Infrastructure.Authentication;
using BallastLane.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

var databaseDirectory = Path.Combine(builder.Environment.ContentRootPath, "data");
Directory.CreateDirectory(databaseDirectory);
var databasePath = Path.Combine(databaseDirectory, "ballast.db");

builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration, databasePath);
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Ballast Lane Tasks API",
        Version = "v1",
        Description = "Task CRUD for the signed-in user, plus registration and login."
    });
    var scheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = "Bearer"
        }
    };
    options.AddSecurityDefinition("Bearer", scheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement { [scheme] = [] });
});

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Jwt settings are missing.");
if (Encoding.UTF8.GetByteCount(jwt.SigningKey) < 32)
{
    throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("client", policy =>
    {
        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.Seed(db, scope.ServiceProvider.GetRequiredService<BallastLane.Application.Abstractions.IPasswordHasher>(), TimeProvider.System);
}

app.UseExceptionHandler();
app.UseCors("client");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var browserDist = Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "..", "..", "client", "dist", "client", "browser"));
PhysicalFileProvider? clientFiles = null;
if (!app.Environment.IsEnvironment("Testing") && Directory.Exists(browserDist))
{
    clientFiles = new PhysicalFileProvider(browserDist);
    app.UseStaticFiles(new StaticFileOptions { FileProvider = clientFiles });
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

if (clientFiles is not null)
{
    app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = clientFiles });
}

app.Run();

public partial class Program;
