# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project overview

This repo is the **Alcance 2 "Core" skeleton** of an academic "Administración de Personal" project
(Colegio Universitario de Cartago). Alcance 1 (a separate, already-complete ASP.NET + Dapper + MySQL
web app) is not in this repo. This repo only contains `AdministracionPersonal.Core`, a single ASP.NET
Core 8 Web API project that will grow into both the Web Service layer and the internal Razor Pages
screens, built in parallel by multiple team members, each owning specific user stories ("Core1"–"Core9").

Full original spec/brief: `brief-alcance2-esqueleto-core.md` (in Spanish). `Reparto.txt` records who owns
which story. Read both if you need the intent behind a stub before implementing it.

## Commands

All commands run from `AdministracionPersonal.Core/` (there is no .sln — it's a single csproj, no need to
`cd` into a solution folder from the repo root).

```
dotnet restore
dotnet build
dotnet run                 # launches on the profile in Properties/launchSettings.json, Swagger at http://localhost:<port>/
dotnet watch run           # hot reload during development
```

There are no automated tests in this repo yet (out of scope per the brief).

Before running locally, set a real MySQL connection string in `appsettings.Development.json`
(`ConnectionStrings:DefaultConnection`) — the checked-in one in `appsettings.json` has placeholder
credentials and `Jwt:Key` is a placeholder too and must be overridden locally.

## Architecture

Single-project layout (deliberately not split into Domain/Application/Infrastructure — the brief allows
this simpler shape as long as each user story has one unambiguous file):

- `Controllers/` — one controller file per group of user stories, **not** one per HTTP verb. The mapping
  is fixed by `Reparto.txt` and documented in each controller's XML summary:
  - `PuestosController.cs` → Core1 (list active puestos)
  - `OferentesController.cs` → Core2 (oferentes aptos for a puesto), Core8 (oferente detail)
  - `EmpleadosController.cs` → Core3 (create empleado from oferente)
  - `AuthController.cs` → Core4 (login)
- `Models/` — plain DTOs returned/consumed by controllers (`ApiResponse<T>`, `PuestoDto`, `OferenteDto`,
  `EmpleadoDto`, etc.) plus `Bitacora.cs`. **Never expose DB entities directly** — controllers must map to
  these DTOs.
- `Services/IBitacoraService` / `BitacoraService` — the one cross-cutting piece every story must call.
  `RegistrarAsync(tipo, entidad, descripcion, idUsuario?, datosAnteriores?, datosNuevos?)` inserts into the
  existing `bitacora` table and **swallows its own exceptions** (logs only) so a bitácora failure never
  breaks the main request flow. Every INSERT/UPDATE/DELETE and most SELECT-by-id endpoints are expected to
  call this.
- `Repositories/IDbConnectionFactory` / `DbConnectionFactory` — hands out a raw `IDbConnection`
  (`MySqlConnection`) for Dapper calls. There is no generic repository/unit-of-work abstraction on top of
  this — controllers (or services they call) use Dapper directly against the connection.
- `Pages/` — Razor Pages for the internal screens (Core5 login, Core6 puestos list, Core7 oferentes list,
  Core9 oferente detail + "crear empleado"). See `Pages/README.md`: whether these stay as Razor Pages here
  or move to a separate frontend project is an explicked-as-pending team decision — check that file before
  assuming the current `.cshtml` stubs are final.

### Data layer

MySQL database `db_personal_sitios` already exists with data (owned by Alcance 1) — **do not create or
migrate tables from this project**. Key tables: `puesto`, `oferente`, `postulacion`, `requisito_puesto`,
`oferente_requisito`, `curriculum_oferente`, `empleado`, `accion_personal`, `usuario`, `bitacora`. Prefer
the existing views over hand-rolled joins:
- `vw_puestos_disponibles`, `vw_oferentes_aptos_puesto`, `vw_detalle_oferente`, `vw_postulaciones_detalle`.

`usuario.password_hash` uses AES-GCM (`AESGCM:iv:tag:cipherdata`, base64 parts joined by `:`) — Core4/Login
must reuse the same verification scheme as Alcance 1, not a new hashing approach.

### Request/response conventions

- Success responses wrap DTOs in `ApiResponse<T>` (`ApiResponse<T>.Ok(data, message)`).
- Failures use `ApiResponse<object>.Fail(message)` with the matching HTTP status (400/401/404/500).
- `Program.cs` registers a global exception handler that turns unhandled exceptions into a 500 JSON
  `{ success, message }` body, hiding the real exception message outside `Development`.
- JWT auth is wired via `Jwt:Key`/`Jwt:Issuer`/`Jwt:Audience`/`Jwt:ExpiresInMinutes` in config; CORS is wide
  open (`AllowAnyOrigin`) since this is an academic project, not production-hardened.
- All endpoints — including unimplemented stubs — must carry `[ProducesResponseType]` attributes and XML
  doc comments so Swagger shows the full contract even before logic exists.

### Stub convention

Unimplemented story endpoints throw `NotImplementedException` with a message naming the story
(e.g. `"Core1: ObtenerPuestosActivos pendiente de implementar."`) and carry a numbered `// TODO Core#:`
comment listing the exact steps from the brief. When implementing a story, follow those steps — they
encode acceptance-criteria details (e.g. bitácora wording, which view to query) that aren't visible
from the code alone.

## Team workflow

Per `Reparto.txt`, stories are owned as: Andrew (Core2, Core7, Core8 — oferentes), Fran (Core1, Core3,
Core6, Core9 — puestos/empleados), Kendall (Core4, Core5 — auth). Work on one story at a time in its own
branch (e.g. `feature/core1-puestos`) to avoid collisions in shared files like `OferentesController.cs`
where two stories live in the same file.
