TECH STACK

A summary of the technologies, libraries, and tooling used across the project.


BACKEND - TaskManagementSystem

   - Language / Runtime: C# on .NET 10 (LTS), ASP.NET Core Web API
   - Architecture: Clean Architecture (Domain <- Application <- Infrastructure / API)
   - ORM / Data access: EF Core 10, code-first migrations
   - Database: SQLite, with an in-memory provider as a zero-config local fallback
   - Authentication: ASP.NET Core Identity + JWT bearer tokens, role claims (Admin / User)
   - Validation: FluentValidation, applied globally via a ValidationFilter
   - Logging: Serilog (console + rolling file sinks)
   - API documentation: Swagger / OpenAPI (Swashbuckle 6.x), JWT auth wired into the UI
   - AI integration: OpenAI (gpt-4o-mini default), powers task-description enhancement
   - Error handling: RFC 7807 ProblemDetails via exception-handling middleware

Backend testing

   - xUnit: test framework
   - Moq: mocking dependencies
   - FluentAssertions: readable assertions
   - Bogus: test data generation
   - EF Core InMemory: repository-level tests

Solution layout

   src/
     TaskManager.Domain/          Entities + enums (no external dependencies)
     TaskManager.Application/     DTOs, services, validators, mapping, interfaces
     TaskManager.Infrastructure/  EF Core, repositories, Identity, migrations
     TaskManager.API/             Controllers, Program.cs, middleware, Swagger
   tests/
     TaskManager.UnitTests/

   Solution file: TaskManager.slnx
   Note: Swashbuckle.AspNetCore is pinned to 6.x because 8.x+ pulls Microsoft.OpenApi
   2.x, which changes the Microsoft.OpenApi.Models API this code uses.


FRONTEND - TaskManagementSystem.FrontEnd

   - Framework: React 19 (functional components + hooks)
   - Build tool: Vite 8 (fast dev server + production builds)
   - Routing: React Router 7 (react-router-dom)
   - HTTP client: axios (talks to the API over JWT)
   - State management: React Context API (AuthContext, TaskContext)
   - Drag-and-drop: @hello-pangea/dnd (persisted task reordering)
   - Linting: ESLint (with React hooks + refresh plugins)
   - Config: VITE_API_URL points the build at the API base URL


DEVOPS AND DEPLOYMENT

   - Containerisation: Docker, separate Dockerfile for API and frontend
   - Orchestration: Docker Compose, one stack per repo
   - API container: .NET runtime + SQLite, data on a persistent "sqlite-data" volume
   - Frontend container: Nginx-served production build, published on port 3000
   - Configuration: environment variables / .env, secrets never committed
   - Version control: Git + GitHub, two repositories (backend + frontend)


WHY THESE CHOICES

   - Clean Architecture + interfaces keep the domain framework-free and the core
     unit-testable.
   - SQLite needs no separate database server (a single file on a volume), while
     EF Core keeps the option open to swap providers.
   - JWT + Identity is the standard, stateless auth model for SPAs talking to a Web API.
   - Vite + React 19 + Context gives a fast, modern, dependency-light frontend without
     the overhead of a larger state library for an app this size.
   - Docker Compose makes the whole system reproducible with a single command per tier.
