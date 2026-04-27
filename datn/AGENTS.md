# Repository Guidelines

This repository is an ASP.NET Core MVC app targeting `.NET 10` with Entity Framework Core (SQL Server), JWT auth (stored in cookies), and SignalR for real-time notifications.

## Project Structure & Module Organization
- `Program.cs`: app startup, DI registration, auth/policies, middleware, SignalR hub mapping.
- `Controllers/`: MVC controllers by feature/role (Manager/Employee/Parent/etc.).
- `Views/`: Razor pages (`.cshtml`) grouped by controller.
- `Models/`: EF entities + view models.
- `Data/AppDbContext.cs`: EF Core model configuration (watch for composite keys).
- `Services/`: business logic and background services (e.g., payroll, token cleanup).
- `Middleware/`: request pipeline components (e.g., refresh token flow).
- `Migrations/`: EF Core migrations (code-first).
- `wwwroot/`: static assets (CSS/JS/libs/uploads).

## Build, Test, and Development Commands
Run from the repo root:
- `dotnet restore`: restore NuGet dependencies.
- `dotnet build`: compile the project.
- `dotnet run`: run the web app.
- `dotnet watch run`: run with hot reload for local development.
- `dotnet ef migrations add <Name>`: create a migration (install `dotnet-ef` if needed).
- `dotnet ef database update`: apply migrations to the configured database.

## Coding Style & Naming Conventions
- Indentation: 4 spaces; keep formatting consistent with existing files.
- C#: `PascalCase` types/methods, `camelCase` locals/params, `Async` suffix for async methods.
- Files: `XController.cs`, `IThingService.cs`, `ThingService.cs`, DTOs in `DTOs/`.
- Prefer the existing MVC + Service-layer pattern; avoid putting heavy logic in controllers.

## Testing Guidelines
There is no dedicated test project in this repo currently. When adding tests, create a separate `*.Tests` project (e.g., `tests/datn.Tests/`) and ensure `dotnet test` passes before opening a PR.

## Commit & Pull Request Guidelines
- Commit messages in history are short and topic-based (e.g., `fix bug`, `UI`, `dashboard - ...`). Keep messages concise and include the feature area when possible.
- Do not commit build artifacts (`bin/`, `obj/`) or IDE/user files (`*.user`, `.vs/`).
- PRs: include a clear description, steps to verify locally, and screenshots for UI/CSS changes (`Views/`, `wwwroot/`).
- For schema changes, include the migration in `Migrations/` and note any required `dotnet ef database update`.

## Security & Configuration Tips
- Configuration lives in `appsettings.json` (`ConnectionStrings:DefaultConnection`, `JwtSettings`). Do not commit production secrets; prefer environment variables or user-secrets for real deployments.
