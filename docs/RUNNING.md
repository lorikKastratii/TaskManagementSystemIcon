HOW TO RUN THE PROJECT

The system has two parts, each in its own repository:

- Backend API  -> repo "TaskManagementSystem" (this repo), .NET 10 Web API + SQLite
- Frontend     -> repo "TaskManagementSystem.FrontEnd", React 19 + Vite

Pick one of the two options below. Docker is the quickest way to see the whole
thing running. Local is best for development.


OPTION A - DOCKER (recommended, zero setup)

Requires Docker Desktop. Run each stack from its own repo root.

1. Backend (API + SQLite), from the TaskManagementSystem repo root:

   docker compose up --build

   - API + Swagger -> http://localhost:5000/swagger
   - SQLite data is stored on the "sqlite-data" volume, so it survives restarts.
   - Roles and demo accounts are seeded automatically on first boot.

2. Frontend, from the TaskManagementSystem.FrontEnd repo root:

   docker compose up --build

   - App -> http://localhost:3000
   - By default it points at the Dockerised API on port 5000 (no config needed).

Open http://localhost:3000, log in with a seeded account (listed below), and you're in.


OPTION B - RUN LOCALLY (no Docker)

1. Backend

   The API runs with zero configuration. With no connection string set it uses an
   in-memory database, so you can start instantly:

   dotnet run --project src/TaskManager.API

   - Swagger -> http://localhost:5062/swagger

   To keep data between restarts, use a SQLite file instead (created on first run):

   PowerShell:
   $env:ConnectionStrings__DefaultConnection="Data Source=taskmanager.db"
   dotnet run --project src/TaskManager.API

   Migrations are applied automatically on startup when a connection string is present.

2. Frontend, from the TaskManagementSystem.FrontEnd repo root:

   cp .env.example .env        (VITE_API_URL defaults to http://localhost:5062/api)
   npm install
   npm run dev

   - App -> the URL Vite prints (typically http://localhost:5173)
   - Make sure VITE_API_URL matches where the backend is running:
       Local backend (dotnet run): http://localhost:5062/api
       Docker backend:             http://localhost:5000/api


SEEDED DEMO ACCOUNTS

Created automatically on every startup. Use these to log in immediately:

   admin@taskmanager.local  /  Admin123!   (Admin)
   test1@taskmanager.local  /  Test123!    (User)
   test2@taskmanager.local  /  Test123!    (User)
   test3@taskmanager.local  /  Test123!    (User)

These are dev/demo credentials. Change or remove the seeding before any real
production use.

You can also register your own account from the UI or via POST /api/auth/register.
Password rules: min 6 chars including an uppercase, a lowercase, and a digit (e.g. Passw0rd).


RUNNING THE TESTS

From the TaskManagementSystem repo root:

   dotnet test tests/TaskManager.UnitTests

Covers the task service (CRUD, ownership, completion invariant, filtering, reordering),
the FluentValidation validators, the mapping logic, and the repository's user-scoping.


API QUICK REFERENCE

All task endpoints require a JWT (click Authorize in Swagger and paste the token,
no "Bearer" prefix).

   POST   /api/auth/register                          Register, returns a JWT
   POST   /api/auth/login                             Log in, returns a JWT
   GET    /api/auth/me                                Current user + roles
   GET    /api/tasks                                  List tasks (?status= ?priority= ?isCompleted= ?search= ?assigneeId=)
   GET    /api/tasks/{id}                             Get one task
   POST   /api/tasks                                  Create
   PUT    /api/tasks/{id}                             Update
   PATCH  /api/tasks/{id}/complete?isCompleted=true   Mark complete/incomplete
   PATCH  /api/tasks/{id}/assign                      Reassign (Admin only)
   PUT    /api/tasks/reorder                          Persist drag-and-drop order
   DELETE /api/tasks/{id}                             Delete
   POST   /api/tasks/enhance-description              AI rewrite of a draft description
   GET    /api/dashboard/stats                        Task statistics for the dashboard
   GET    /api/users                                  List users (Admin only)


COMMON PORTS AT A GLANCE

   API / Swagger
       Local:  http://localhost:5062/swagger
       Docker: http://localhost:5000/swagger

   Frontend
       Local:  http://localhost:5173
       Docker: http://localhost:3000
