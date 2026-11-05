# Generating the task API

The running API is the reviewed result. This note is the prompt, a representative first draft, and the corrections that made it into the solution.

## Prompt

```text
Build an ASP.NET Core Web API for a personal task list.

Rules:
- Clean Architecture: Domain, Application, Infrastructure, and an HTTP API.
- Domain has no framework references. Application does not reference EF Core or ASP.NET.
- Tasks have title, description, status (Todo, InProgress, Done), and due date.
- Users are stored separately. Register, login, and a protected "me" endpoint.
- JWT bearer auth. Task endpoints require a token. Health, register, and login do not.
- A user can only read or change their own tasks. Missing and forbidden tasks both return 404.
- Validate in the application layer and return field errors. Do not trust data annotations alone.
- Hash passwords. Never return a hash or the raw password.
- Login must not reveal whether the email exists.
- SQLite via EF Core, with a migration and an idempotent demo seed.
- xUnit tests for rules, persistence, HTTP, and project dependencies.
- Controllers stay thin and do not inject DbContext.
```

## Representative first draft

This is the shape a first pass tends to produce. It was not kept. Persistence, hashing, and HTTP status codes are mixed into one controller, and any caller can read every task.

```csharp
[ApiController]
[Route("api/tasks")]
public class TasksController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<List<TaskItem>> Get() => await db.Tasks.ToListAsync();

    [HttpPost]
    public async Task<TaskItem> Post(TaskItem task)
    {
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }
}
```

A matching auth draft checks the email and password in the action, returns 401 for an unknown email and 400 for a wrong password, and sends the user row — including `PasswordHash` — back to the client.

## What was validated

- `dotnet test` covers field rules, ownership, duplicate emails, the unique index, the demo seed, and the HTTP status codes.
- Architecture tests fail if Domain or Application reference EF Core, or if a controller references Infrastructure.
- Login and register responses were checked so the password and hash are absent.
- A second user cannot read, update, or delete the first user's task (API test and application test).

## What changed

- Validation and ownership moved to `TaskService` and `AccountService`. Controllers only translate HTTP.
- Repository interfaces sit in Application. EF Core implements them in Infrastructure.
- Unknown email and wrong password both return 401 with `Invalid email or password.`
- Another user's task returns 404, not 403.
- Register conflicts return 409, including the unique-index race in `UnitOfWork`.
- Status accepts only the names `Todo`, `InProgress`, and `Done`. The number `1` and the lowercase `todo` are rejected.
- The Angular client talks to these contracts and shows the field errors. It does not decide who owns a task.
