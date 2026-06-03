# Task Management System — Backend (API)

The backend for a full-stack task manager built for the **ICON Studios** full-stack developer trial.
Users register, log in, and manage their own tasks — create, edit, delete, mark complete/incomplete,
filter by status/priority/search, and reorder by drag-and-drop.

- **Backend (this repo):** .NET 10 ASP.NET Core Web API · Clean Architecture · EF Core + SQL Server · JWT auth · FluentValidation · Serilog · Swagger
- **Frontend (separate repo):** React 19 + Vite — see `TaskManagementSystem.FrontEnd`
- **Deployment:** Docker Compose (SQL Server + API)

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
| SQL Server     | localhost:1433 (sa / TaskManager2026!) |

The API waits for SQL Server to be healthy, then applies EF Core migrations automatically.
Open Swagger, **register an account**, authorise, and exercise the endpoints — or run the
frontend repo against it.

To override the default dev secrets, copy `.env.example` to `.env` and edit it.

---

## Running locally (without Docker)

### Backend

The API runs with **zero configuration** out of the box: when no connection string is set it uses an
in-memory database, so you can start immediately.

```bash
dotnet run --project src/TaskManager.API
```

Swagger UI: **http://localhost:5062/swagger**

To use a real SQL Server instead, start one (e.g. `docker compose -f docker-compose.local.yml up -d`)
and provide the connection string:

```bash
# PowerShell
$env:ConnectionStrings__DefaultConnection="Server=localhost,1433;Database=TaskManager;User Id=sa;Password=TaskManager2026!;TrustServerCertificate=True;Encrypt=False"
dotnet run --project src/TaskManager.API
```

Migrations are applied automatically on startup when a SQL Server connection string is present.

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
Dockerfile · docker-compose.yml · docker-compose.local.yml
```

> The React frontend (`TaskManagementSystem.FrontEnd`) is maintained in a separate repository.

See [CLAUDE.md](CLAUDE.md) for architecture notes and conventions.

---

## Configuration reference

| Setting | Default | Notes |
|---------|---------|-------|
| `ConnectionStrings:DefaultConnection` | *(empty)* | Empty ⇒ in-memory DB; set for SQL Server |
| `Jwt:Key` | dev key in `appsettings.Development.json` | **Override in production** (≥32 chars) |
| `Jwt:Issuer` / `Jwt:Audience` | `TaskManager.API` / `TaskManager.Client` | |
| `Jwt:ExpiryHours` | `12` | Token lifetime |
| `Cors:AllowedOrigins` | *(empty ⇒ allow any)* | Lock down in production |
| `VITE_API_URL` (frontend) | `http://localhost:5062/api` | API base URL baked into the build |
