COMPLETED FEATURES AND BONUS POINTS

This document maps what was built against the ICON Studios Full-Stack Trial brief
(Full Stack Trial.pdf).


CORE REQUIREMENTS - all complete

Backend (.NET Core)

   - RESTful API with full CRUD on tasks: create, read, update, delete, plus mark
     complete/incomplete and reorder.
   - Database: EF Core 10 with SQLite (code-first migrations applied automatically
     on startup). An in-memory provider is used as a zero-config local fallback.
   - Data validation: FluentValidation on every incoming request via a global
     ValidationFilter.
   - Swagger / OpenAPI docs: Swashbuckle, with JWT auth wired into the Swagger UI.

Frontend (React)

   - Responsive pages built with React 19 functional components and hooks: task
     listing, add/edit, and status/priority/search filtering.
   - API integration via axios.
   - State management via the React Context API (AuthContext, TaskContext).
   - Responsive UI across devices.

General

   - Documentation: README, CLAUDE.md, and this docs folder explain how to run and
     access every service.
   - Clean coding practices: Clean Architecture, meaningful comments, consistent
     conventions.


BONUS POINTS - all 5 listed bonuses complete

The brief's Evaluation Criteria names exactly five bonus points. Every one is done:

   1. Authentication
      ASP.NET Core Identity + JWT bearer tokens. Register/login (/api/auth), password
      policy enforced via FluentValidation, tokens carry role claims.

   2. Task prioritisation
      TaskPriority enum on TaskItem, persisted as a string, settable on create/update
      and filterable (?priority=).

   3. Drag-and-drop sorting
      Persisted ordering via PUT /api/tasks/reorder. Frontend uses @hello-pangea/dnd
      for the drag-and-drop board.

   4. Unit tests
      xUnit + Moq + FluentAssertions + Bogus. Covers the task service (CRUD, ownership,
      completion invariant, filtering, reordering), validators, mapping, and repository
      user-scoping.

   5. Containerisation
      Dockerfile + docker-compose.yml for both the API (with SQLite on a persistent
      volume) and the frontend (Nginx-served build). One command per stack:
      docker compose up --build.


BEYOND THE BRIEF - advanced functionality

The brief also rewards "advanced functionality". In addition to the five bonuses:

   - Role-based access control: Admin / User roles seeded on startup. The API enforces
     [Authorize(Roles = "Admin")]; the frontend adapts its UI from the user's roles.
   - Task assignment: tasks have an owner plus a nullable assignee. Admins can reassign
     (PATCH /api/tasks/{id}/assign) and filter by assignee (?assigneeId=).
   - AI task-description enhancement: POST /api/tasks/enhance-description rewrites a
     draft description (OpenAI); gracefully disabled with a friendly 400 when no API
     key is configured.
   - Dashboard statistics: GET /api/dashboard/stats powers an at-a-glance summary.
   - Clean Architecture: Domain <- Application <- Infrastructure/API, with the
     Unit-of-Work pattern and repository interfaces keeping the core unit-testable.
   - Structured logging: Serilog (console + rolling file).
   - Consistent error handling: services throw domain exceptions that middleware maps
     to RFC 7807 ProblemDetails responses.
   - Persistent + zero-config data: SQLite on a Docker volume in production, in-memory
     fallback for instant local startup.


SUMMARY

   - Core backend requirements:    complete (4/4)
   - Core frontend requirements:   complete (4/4)
   - Listed bonus points:          complete (5/5)
   - Extra advanced functionality: 7+ additional features
