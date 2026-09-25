# Phonebook Application — .NET Port

A contact management application. This project is a port of a working Vue 3 / PostgreSQL / Playwright phonebook to a **.NET 8 / ASP.NET Core** backend. The frontend, REST contract, database model, and end-to-end tests are preserved. The earlier FastAPI and Java/Spring Boot backends are replaced with ASP.NET Core Minimal APIs, Entity Framework Core, and Npgsql.

The application uses **Nginx as a reverse proxy**, exposing the Vue frontend and .NET API through a single origin. This removes the need for browser-side CORS configuration.

## What it does

The app is a phonebook / contact workspace:

- Create, view, edit, and delete contacts
- Name, phone number, email, address, tags, favorite flag, and timestamps
- Favorites and recently viewed lists
- Tags that can be shared across contacts (search and filter by tag)
- Universal search across name, phone number, and tag
- Filters: favorite, tag, unlabeled, recently viewed, search
- Sorting: name A–Z / Z–A, recently viewed, recently added, recently updated
- Pagination with page numbers, next/previous, and a page input
- CSV / XLSX / XLS import with preview, validation, duplicate detection, tags, and unrelated-column ignoring
- CSV export including tags
- Light / dark / system theme
- Responsive desktop and mobile layout

The sidebar is intentionally limited to **All Contacts**, **Favorites**, **Recently viewed**, and **Settings**. Tags are **not** shown in the sidebar; they are managed on contacts and in the tag filter.

## Architecture

The application uses Nginx as the single public entry point.

```text
                         Browser
                            │
                            │ http://localhost:8080
                            ▼
                      ┌───────────┐
                      │   Nginx   │
                      │    :80    │
                      └─────┬─────┘
                            │
                 ┌──────────┴──────────┐
                 │                     │
              /                        /api/*
                 │                     │
                 ▼                     ▼
          ┌─────────────┐       ┌──────────────┐
          │   Vue 3     │       │  .NET 8      │
          │    :5173    │       │   :8000      │
          └─────────────┘       └──────┬───────┘
                                       │
                                  EF Core / Npgsql
                                       │
                                       ▼
                                ┌──────────────┐
                                │  PostgreSQL  │
                                │     :5432    │
                                └──────────────┘
```

The browser communicates only with the Nginx origin:

```text
http://localhost:8080
```

Frontend requests use the `/api` prefix:

```text
http://localhost:8080/api/contacts/
```

Nginx routes `/api/*` requests to the .NET backend and routes frontend requests to Vue.

The .NET API itself continues to use routes such as:

```text
/contacts/
/contacts/{id}
/contacts/tags
/contacts/metrics
```

Nginx removes the `/api` prefix before forwarding the request to the .NET backend.

Because the browser communicates with both the frontend and API through the same origin, the backend does not require a browser-facing CORS configuration.

### Technologies

| Layer | Stack |
| --- | --- |
| Frontend | Vue 3, Vite, Pinia, Vue Router, Axios |
| Reverse Proxy | Nginx |
| Backend | .NET 8, ASP.NET Core Minimal APIs, Entity Framework Core, Npgsql, DataAnnotations validation |
| Database | PostgreSQL 16 |
| Tests | Playwright |
| Containers | Docker, Docker Compose |

### Project structure

```text
PhoneBookAppli-DotNet/

├── backend/                 .NET 8 API
│   ├── Endpoints/           Minimal API route definitions
│   ├── Services/
│   ├── Data/                EF Core DbContext + model configuration
│   ├── Models/              Entity classes (Contact, Tag)
│   ├── Dtos/                Request/response records
│   ├── Validation/          Request body/query binding + validation
│   ├── Middleware/           Error handling, {"detail"} responses, date format
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Phonebook.csproj
│   └── Dockerfile

├── frontend/                Existing Vue 3 UI

├── nginx/
│   └── nginx.conf           Nginx reverse proxy configuration

├── playwright/              Existing Playwright suite

├── docker-compose.yml

├── .env.example

└── README.md
```

## Prerequisites

- Docker Desktop or Docker Engine with Docker Compose
- Node.js 22+ for local frontend / Playwright development
- Optional for host-side backend development: .NET 8 SDK

The recommended way to run the complete application is through Docker Compose.

## Environment variables

Copy `.env.example` to `.env`:

```bash
cp .env.example .env
```

Example:

```dotenv
POSTGRES_DB=phonebook
POSTGRES_USER=phonebook
POSTGRES_PASSWORD=change-me
POSTGRES_PORT=5432
```

| Variable | Purpose |
| --- | --- |
| `POSTGRES_DB` | PostgreSQL database name |
| `POSTGRES_USER` | PostgreSQL username |
| `POSTGRES_PASSWORD` | PostgreSQL password |
| `POSTGRES_PORT` | Host port mapped to PostgreSQL (5432 inside Compose) |

The backend container receives its database connection settings through the Docker Compose environment (`DATABASE_HOST`, `DATABASE_PORT`, `DATABASE_NAME`, `DATABASE_USER`, `DATABASE_PASSWORD`), with the same names and defaults the earlier backends used.

## Docker setup

From the project root:

```bash
docker compose up --build
```

The application is accessed through Nginx:

```text
http://localhost:8080
```

### Application URLs

| Service | URL | Purpose |
| --- | --- | --- |
| Nginx | http://localhost:8080 | Main application entry point |
| Frontend | Internal Docker service on `frontend:5173` | Vue application |
| Backend | Internal Docker service on `backend:8000` | .NET 8 REST API |
| PostgreSQL | Internal Docker service on `db:5432` | Database |

The frontend and backend are intentionally routed through Nginx rather than being directly exposed to the browser. Only Nginx publishes a host port (8080); the backend's port 8000 stays internal to the Docker network.

Stop the application with:

```bash
docker compose down
```

Data is stored in a dedicated Docker volume for this Compose project (`phonebook-dotnet_postgres_data`). This project deliberately does not share a database with the earlier Java or FastAPI projects — the backend creates its own schema (tables `contacts`, `tags`, `contact_tags`) on first start.

To remove the containers and database volume:

```bash
docker compose down -v
```

`docker compose down -v` removes the PostgreSQL data stored in the Docker volume.

If the configured host port is already in use (for example by another Postgres container), set `POSTGRES_PORT` to another available host port in `.env`. The PostgreSQL container continues to use port 5432 internally.

## Local development

### PostgreSQL

PostgreSQL can be started using Docker Compose:

```bash
docker compose up db
```

or PostgreSQL can be run locally with the credentials configured in `.env`.

### Backend

With the .NET 8 SDK installed:

```bash
cd backend
dotnet run
```

The API listens on port 8000 (configured by `Urls` in `appsettings.json`; `DATABASE_*` environment variables override the database defaults).

Package without running:

```bash
cd backend
dotnet build
```

Without a local SDK, the backend can be built in Docker:

```bash
docker compose build backend
```

### Frontend

```bash
cd frontend
npm install
npm run dev
```

The Vue development server listens on port 5173.

For the complete Docker-based application, Nginx is the public entry point and handles the reverse proxy between the frontend and backend.

## Nginx reverse proxy

Nginx provides a single-origin entry point for the application.

Requests are routed according to their path:

```text
Browser
   │
   ├── /              → Vue frontend
   │
   └── /api/*         → .NET backend
```

For example:

```text
GET /api/contacts/
```

is received by Nginx and forwarded internally as:

```text
GET /contacts/
```

to:

```text
backend:8000
```

The browser therefore does not directly communicate with the backend on port 8000.

This architecture allows the Vue frontend and API to be served from the same origin without requiring browser-facing CORS configuration.

## Playwright

Playwright tests are configured to use the Nginx entry point:

```text
http://127.0.0.1:8080
```

The test flow is:

```text
Playwright
    │
    ▼
Nginx :8080
    │
    ├── Vue :5173
    │
    └── .NET :8000
             │
             ▼
          PostgreSQL
```

Install the Playwright dependencies:

```bash
cd playwright
npm install
npx playwright install chromium
```

Run the complete test suite (the stack must already be running):

```bash
npx playwright test --reporter=list
```

The suite covers:

- Contact CRUD
- Frontend validation
- Search by name, phone, and tag
- Combined search and tag filtering
- Favorites
- Recently viewed
- Sorting
- Pagination
- CSV/XLSX/XLS import
- Import validation and duplicate handling
- Drag-and-drop import
- CSV export
- Appearance settings
- Responsive layout
- 1000-contact dataset handling
- Application smoke tests

The Playwright suite uses a single worker for deterministic execution.

## API overview

All JSON fields use snake_case. Errors use:

```json
{
  "detail": "..."
}
```

| Method | Path | Purpose |
| --- | --- | --- |
| GET | `/` | Health message |
| GET | `/contacts/` | List contacts |
| POST | `/contacts/` | Create contact |
| GET | `/contacts/{id}` | Get contact |
| PUT | `/contacts/{id}` | Update contact |
| DELETE | `/contacts/{id}` | Delete contact |
| PATCH | `/contacts/{id}/favorite` | Update favorite status |
| PATCH | `/contacts/{id}/viewed` | Mark contact as recently viewed |
| GET | `/contacts/tags` | Get tag list |
| GET | `/contacts/metrics` | Get dashboard metrics |
| POST | `/contacts/import` | Import contacts |

When accessed through Nginx, the API is available under:

```text
/api/contacts/
```

Nginx removes the `/api` prefix before forwarding the request to the backend.

### Contact list query parameters

`GET /contacts/` supports:

- `search`
- `favorite`
- `tag`
- `unlabeled`
- `recent`
- `sort`
- `page`
- `limit`

List `sort` values:

- `name_asc`
- `name_desc`
- `recently_viewed`
- `recently_added`
- `recently_updated`

Duplicate phone numbers or emails return HTTP 400 with:

```json
{
  "detail": "Phone number or email already exists"
}
```

## Features

- Contact CRUD with tags
- Favorites filter and star toggle
- Recently viewed timestamp
- Search by name, phone, or tag without a full page reload
- Tag filter combined with search
- Untagged filter
- Configurable page size in Settings
- Import CSV/XLSX/XLS via file picker or drag-and-drop
- Export of the current filtered contact set as `phonebook-contacts.csv`
- Light, dark, and system appearance settings
- Responsive desktop and mobile interface

## Import / export

Import is parsed in the browser using CSV/Excel processing.

Supported fields:

- `name`
- `phone_number`
- `email`
- `address`
- `tags`

Unrelated columns are ignored.

The frontend validates imported rows before sending them to:

```text
POST /contacts/import
```

The backend inserts valid contacts and reports skipped duplicates. Rows are processed independently: an invalid or duplicate row is skipped and reported while the remaining rows continue.

Export downloads a UTF-8 CSV with BOM containing the contacts matching the current search and filters, including a tags column.

## Database

The application uses PostgreSQL with the following tables:

**contacts**

- `id`
- `name`
- `phone_number`
- `email`
- `address`
- `is_favorite`
- `last_viewed_at`
- `created_at`
- `updated_at`

`phone_number` is unique and `email` is unique when provided.

**tags**

- `id`
- `name`

Tag names are unique.

**contact_tags**

Many-to-many join table connecting contacts and tags.

Entity Framework Core uses:

```text
EnsureCreated()
```

to create the database schema when the application starts (a no-op on an existing database, matching the previous `ddl-auto=update` behavior for a fresh, application-managed schema).

## .NET port notes

This repository is a port rather than a redesign.

- The Vue UI and routes are preserved
- The existing REST contract is preserved
- The PostgreSQL database model is preserved
- JSON field names remain snake_case (ASP.NET Core `SnakeCaseLower` naming policy)
- HTTP status codes and `{"detail": ...}` error responses match the original API behavior
- Validation error wording matches the previous API (including field names such as `phone_number`)
- Contact timestamps are serialized as local ISO datetimes without a timezone suffix, exactly as before
- Tags remain off the sidebar
- Import is still parsed in the frontend; the backend receives already-parsed rows
- ASP.NET Core Minimal APIs replace the original FastAPI / Spring MVC routing layer
- EF Core with Npgsql replaces SQLAlchemy / Hibernate
- Nginx provides the single public entry point for the frontend and API
- Browser-facing CORS configuration is not required because frontend and API use the same origin
- Playwright verifies the application behavior end-to-end through the Nginx entry point
