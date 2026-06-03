# Task Management System — Backend (API)

The backend for a full-stack task manager built for the **ICON Studios** full-stack developer trial.
Users register, log in, and manage their own tasks — create, edit, delete, mark complete/incomplete,
filter by status/priority/search, and reorder by drag-and-drop.

- **Backend (this repo):** .NET 10 ASP.NET Core Web API · Clean Architecture · EF Core + SQLite · JWT auth · FluentValidation · Serilog · Swagger
- **Frontend (separate repo):** React 19 + Vite — see `TaskManagementSystem.FrontEnd`
- **Deployment:** Docker Compose (API + SQLite) — single container, data on a persistent volume

Bonus features implemented: **authentication, task prioritisation, drag-and-drop ordering, unit tests, containerisation.**

> The React frontend lives in its own repository (`TaskManagementSystem.FrontEnd`) so the two
> tiers can be versioned and deployed independently. Point the frontend's `VITE_API_URL` at this API.

---

## Quick start with Docker (recommended)

Requires Docker Desktop.

```bash
docker compose up --build
```

| Service        | URL                                |
|----------------|------------------------------------|
| API + Swagger  | http://localhost:5000/swagger      |

The API uses **SQLite** — no separate database server. On startup it applies EF Core migrations
automatically and creates the database file. In Docker the file lives at `/data/taskmanager.db`
on the `sqlite-data` volume, so your data survives container rebuilds and restarts.
Open Swagger, **register an account**, authorise, and exercise the endpoints — or run the
frontend repo against it.

To override the default dev secrets, copy `.env.example` to `.env` and edit it.

---

## Deploying

The app ships as a single container (the bundled `Dockerfile`) with SQLite for storage — no separate
database server to provision. Because SQLite keeps data in a file, the one rule that matters in any
hosting environment is: **put the database file on persistent storage**, otherwise it resets whenever
the container is rebuilt or restarted.

1. **Build the image** from the included `Dockerfile`. The app listens on port `8080`
   (`ASPNETCORE_URLS=http://+:8080`).
2. **Mount a persistent volume** and point the connection string at a file on it.
3. **Set environment variables:**

   | Variable | Value |
   |----------|-------|
   | `ConnectionStrings__DefaultConnection` | `Data Source=/data/taskmanager.db` (a path on the volume) |
   | `Jwt__Key` | a long random string (≥32 chars) |
   | `Cors__AllowedOrigins__0` | your deployed frontend origin |
   | `ASPNETCORE_ENVIRONMENT` | `Docker` *(skips HTTPS redirection when TLS is terminated by a proxy)* |

4. **Run it.** On first boot the API creates the database file on the volume, applies migrations, and
   seeds the roles/accounts. Swagger is at `/swagger` on your deployed URL.

`docker compose up --build` does all of this locally — it already mounts the `sqlite-data` volume at
`/data` for you.

> The seeded demo accounts (`admin@taskmanager.local` / `Admin123!`, etc.) are created on every startup.
> Change or remove the seeding before any real production use.

---

## Running locally (without Docker)

### Backend

The API runs with **zero configuration** out of the box: when no connection string is set it uses an
in-memory database, so you can start immediately.

```bash
dotnet run --project src/TaskManager.API
```

Swagger UI: **http://localhost:5062/swagger**

To use a persistent **SQLite** file instead (data survives restarts), point the connection string at a
file path — the database and schema are created automatically on first run:

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection="Data Source=taskmanager.db"
dotnet run --project src/TaskManager.API
```

Migrations are applied automatically on startup whenever a connection string is present.

### Frontend

The frontend is a separate repository — see `TaskManagementSystem.FrontEnd`. Clone it, set its
`VITE_API_URL` to this API's base URL (default `http://localhost:5062/api`), then `npm install && npm run dev`.

---

## Using the API

1. `POST /api/auth/register` with `{ "email", "password", "confirmPassword" }` → returns a JWT.
   Password rules: min 6 chars incl. uppercase, lowercase and a digit (e.g. `Passw0rd`).
2. In Swagger, click **Authorize** and paste the token (no `Bearer ` prefix).
3. Use the `/api/tasks` endpoints:

| Method | Route | Purpose |
|--------|-------|---------|
| GET    | `/api/tasks` | List tasks (`?status=`, `?priority=`, `?isCompleted=`, `?search=`) |
| GET    | `/api/tasks/{id}` | Get one task |
| POST   | `/api/tasks` | Create |
| PUT    | `/api/tasks/{id}` | Update (partial) |
| PATCH  | `/api/tasks/{id}/complete?isCompleted=true` | Mark complete/incomplete |
| PUT    | `/api/tasks/reorder` | Persist drag-and-drop order (`{ "orderedTaskIds": [...] }`) |
| DELETE | `/api/tasks/{id}` | Delete |

All task endpoints require authentication and only ever return the caller's own tasks.

---

## Running the tests

```bash
dotnet test tests/TaskManager.UnitTests
```

Covers the task service (CRUD, ownership, completion invariant, filtering, reordering),
the FluentValidation validators, the mapping logic, and the repository's user-scoping (EF InMemory).

---

## Project structure

```
src/
  TaskManager.Domain/          # Entities + enums (no dependencies)
  TaskManager.Application/     # DTOs, services, validators, mapping, interfaces
  TaskManager.Infrastructure/  # EF Core, repositories, Identity, migrations
  TaskManager.API/             # Controllers, Program.cs, middleware, Swagger
tests/
  TaskManager.UnitTests/
Dockerfile · docker-compose.yml
```

> The React frontend (`TaskManagementSystem.FrontEnd`) is maintained in a separate repository.

See [CLAUDE.md](CLAUDE.md) for architecture notes and conventions.

---

## Configuration reference

| Setting | Default | Notes |
|---------|---------|-------|
| `ConnectionStrings:DefaultConnection` | *(empty)* | Empty ⇒ in-memory DB; set to `Data Source=<path>.db` for SQLite |
| `Jwt:Key` | dev key in `appsettings.Development.json` | **Override in production** (≥32 chars) |
| `Jwt:Issuer` / `Jwt:Audience` | `TaskManager.API` / `TaskManager.Client` | |
| `Jwt:ExpiryHours` | `12` | Token lifetime |
| `Cors:AllowedOrigins` | *(empty ⇒ allow any)* | Lock down in production |
| `VITE_API_URL` (frontend) | `http://localhost:5062/api` | API base URL baked into the build |
