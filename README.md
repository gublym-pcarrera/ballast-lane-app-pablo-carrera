# Tasks

A small task list for the Ballast Lane .NET technical exercise. Signed-in users create, read, update, and delete their own tasks. Accounts live in a separate table from tasks.

## User story

As a signed-in user, I manage my own tasks — title, description, status, and due date — so I can track what I owe and when it is due. I can register, sign in, and I only see my own tasks.

## Demo account

| | |
| --- | --- |
| Email | `demo@ballastlane.com` |
| Password | `Demo1234!` |

The API creates this account, and three sample tasks, the first time it starts. The sign-in screen can fill these credentials.

## Run it

You need the [.NET 9 SDK](https://dotnet.microsoft.com/download) and Node.js `22.22.3` (see `.nvmrc`).

```bash
dotnet test
dotnet run --project src/BallastLane.Api
```

The API listens on `http://localhost:5080`. Swagger is at `http://localhost:5080/swagger` while the environment is Development.

In a second terminal:

```bash
cd client
npm install
npm start
```

Open `http://localhost:4200`. The dev server proxies `/api` to the API.

To serve the built UI from the API instead:

```bash
cd client
npm run build
cd ..
dotnet run --project src/BallastLane.Api
```

Then open `http://localhost:5080`.

## Demo script

1. Sign in with the demo account and show the three seeded tasks.
2. Add a task, change its status, filter the list, and delete it.
3. Sign out, register a new account, and show that the demo tasks are not visible.
4. Open Swagger, call `GET /api/tasks` without a token, then repeat it with the bearer token from login.
5. Walk the projects below and the tests that lock the rules in place.

## Architecture

```text
src/BallastLane.Domain            entities only
src/BallastLane.Application       rules, use cases, repository ports
src/BallastLane.Infrastructure    EF Core, SQLite, password hashing, JWT issuing
src/BallastLane.Api               HTTP controllers and composition root
client                            Angular 22 UI
```

Domain and application code do not reference EF Core or ASP.NET. Controllers depend on application services and pass the signed-in user id in. They do not touch the database. `tests/BallastLane.Architecture.Tests` fails the build if those references appear.

Business rules live in `AccountRules` and `TaskRules`: required fields, lengths, the status values `Todo`, `InProgress`, and `Done`, email shape, and a password that is 8–128 characters with a letter and a number. A new task is always `Todo`; status can change when the task is edited. A due date must be today or later. A user who asks for someone else's task gets **404**, so the API does not reveal that the row exists. Login failures use one message for an unknown email and a wrong password. Passwords are stored with the ASP.NET password hasher and are not returned by the API.

| Method | Path | Auth |
| --- | --- | --- |
| `GET` | `/api/health` | Public |
| `POST` | `/api/auth/register` | Public |
| `POST` | `/api/auth/login` | Public |
| `GET` | `/api/auth/me` | Bearer JWT |
| `GET` | `/api/tasks` | Bearer JWT. Optional `?status=` |
| `GET` | `/api/tasks/{id}` | Bearer JWT |
| `POST` | `/api/tasks` | Bearer JWT |
| `PUT` | `/api/tasks/{id}` | Bearer JWT |
| `DELETE` | `/api/tasks/{id}` | Bearer JWT |

SQLite is created at `src/BallastLane.Api/data/ballast.db`. That folder is gitignored. The signing key in `appsettings.json` is for this local demo only.

The notes on how the API was prompted, reviewed, and corrected are in [docs/genai.md](docs/genai.md).
