# AGENTS.md

## What this repo actually is

- The backend is **.NET 8 / ASP.NET Core** in `backend/` (project file `backend/Phonebook.csproj`). It is a port of earlier FastAPI and Java/Spring Boot backends; the Vue frontend, REST contract, DB model, and Playwright suite were preserved deliberately. The Java implementation lives in a separate repository and is only a reference — do not copy Java files back in.
- Four services, one public origin: **Nginx (:8080) → Vue dev server (:5173) for `/`, .NET backend (:8000) for `/api/*`**. Nginx strips the `/api` prefix, so the backend routes are `/contacts/...` (no `/api`). The backend port is internal to the Docker network only.
- No CI workflows, no lint/format/typecheck config, no backend unit tests. The only tests are Playwright e2e, plus the on-demand API contract script used during the port (not committed).

## Commands

```bash
# Full stack (from repo root; requires .env — copy from .env.example)
docker compose up --build                      # app at http://localhost:8080

# Backend (needs .NET 8 SDK locally, or build via Docker — no SDK is installed on some machines)
cd backend && dotnet run                       # port 8000 (Urls in appsettings.json)
cd backend && dotnet build                     # build/verify backend
docker compose build backend                   # build backend image without a local SDK

# Frontend only
cd frontend && npm install && npm run dev      # port 5173

# E2E tests — stack must already be running at http://127.0.0.1:8080
# IMPORTANT: run from the playwright/ directory (repo root has no Playwright config)
cd playwright && npm install && npx playwright install chromium
npx playwright test --reporter=list            # full suite (npm test does the same)
npx playwright test tests/contacts.spec.js     # single file
```

- Verification order: `dotnet build` (backend) → `npm run build` (frontend) → Playwright with the full stack up.
- `playwright/workers: 1` is intentional (deterministic runs against a shared DB) — don't raise it.
- Compose project name is `phonebook-dotnet`, giving this project its **own** volume (`phonebook-dotnet_postgres_data`) — deliberately separate from the Java project's database. Don't rename it back or share volumes.
- `playwright/start-api.mjs` starts only `db` + `backend` via Compose and refuses to run if port 8000 is held by a non-Phonebook process; tests still need frontend + nginx up for baseURL `127.0.0.1:8080`.

## Contract conventions (preserved on purpose — don't "modernize")

- JSON fields are snake_case (`JsonNamingPolicy.SnakeCaseLower` in `Program.cs`).
- Errors are `{"detail": "..."}` — produced by `Middleware/ExceptionHandlingMiddleware.cs` (exceptions) and `Middleware/ErrorResponses.cs` (model-state failures). Not ProblemDetail/RFC 7807; don't switch to `UseExceptionHandler`/ProblemDetails.
- Duplicate phone/email → HTTP 400 with `Phone number or email already exists` (thrown by the service *and* by the middleware on any `DbUpdateException`, mirroring the old catch-all).
- List `sort` values: `name_asc`, `name_desc`, `recently_viewed`, `recently_added`, `recently_updated`. Full API reference is in `README.md`.
- Datetimes serialize **without** timezone suffix (`Middleware/DateTimeJsonConverter.cs`) — the frontend parses them as local time. Never emit `Z`/offsets.
- No CORS config: browser talks to a single Nginx origin. Don't add CORS policies/headers.
- Trailing-slash routes (`/contacts/` and `/contacts`) and query param names are consumed by the existing frontend and tests; changing them breaks e2e.

## Backend quirks

- DB config comes from `DATABASE_HOST/PORT/NAME/USER/PASSWORD` env vars (see `.env.example`), read in `Program.cs`; defaults live in `appsettings.json`.
- Schema is created with EF Core `EnsureCreated()` on startup — there is **no migration tool** (no Flyway, no EF migrations). For this project (fresh dedicated DB) that replaces `ddl-auto=update`.
- Endpoint routes in `Endpoints/ContactEndpoints.cs` use `{id:long}` constraints so `/contacts/tags` and `/contacts/metrics` aren't swallowed by `/{id}` — keep that pattern for new routes.
- The 400 body is assembled by `Middleware/ErrorResponses.cs` from errors collected in `Validation/` (previously `InvalidModelStateResponseFactory`) — keep it, or the error shape breaks.
- Validation messages are hand-set on the DTO attributes to match the previous API's exact wording (`must not be blank`, `size must be between 0 and 255`, `must be a well-formed email address`, `must not be null`), and `ErrorResponses.JsonField()` maps field names back (`PhoneNumber` → `phone_number`, `IsFavorite` → `is_favorite`, nested rows keep `rows[0].phoneNumber`).
- Business rules and their exact error strings live in `Services/ContactService.cs` (phone validation, tag rules, sort whitelist, import row handling).
- README documents deliberate product constraints (sidebar limited to All Contacts / Favorites / Recently viewed / Settings; tags off the sidebar; JSON/import field names like `phone_number`).

## Frontend quirks

- `frontend/src/services/api.js` hardcodes `baseURL: "/api"`. The `VITE_API_BASE_URL` / `VITE_API_PROXY_TARGET` vars set in `docker-compose.yml` are not read anywhere — don't rely on them.
- CSV/XLSX/XLS import is parsed **in the browser**; the backend only receives already-parsed rows at `POST /contacts/import`.

## Testing quirks

- Playwright hits the app through Nginx (`http://127.0.0.1:8080`); API helpers use `PHONEBOOK_API_URL` (default `http://localhost:8080/api`).
- Tests share live DB state: helpers in `playwright/tests/helpers.js` create uniquely-named contacts and clean up. Seed/pagination tests create up to ~1000 contacts — runs are slow by design (timeout 120s/test).
- If a test fails: investigate the .NET backend against the Java reference first; never edit a test to accommodate backend drift (requires explicit user approval).
- More app/feature documentation lives in `README.md`; trust config/scripts over prose if they conflict.
