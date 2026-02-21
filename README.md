# 🚩 Feature Flag Engine

A feature flag system that lets you turn features on or off for specific users, groups, or regions — without redeploying your application.

**Think of it like a light switch for features.** You can flip a feature on globally, or only for certain people, teams, or geographies.

## What Does It Do?

- **Create feature flags** with a name, description, and an on/off default
- **Toggle features** on or off globally, or override them for specific users, groups, or regions
- **Evaluate in real time** — ask "is this feature on for Alice in the beta group in Europe?" and get a yes/no answer instantly

### How Does It Decide?

When you ask "is feature X enabled?", it checks in this order:

```
1. Is there a rule for this specific user?         → Use it
2. Is there a rule for this user's group?           → Use it
3. Is there a rule for this user's region?          → Use it
4. None of the above?                               → Use the global default
```

The most specific rule always wins.

## How to Run It

You need [.NET 10 SDK](https://dotnet.microsoft.com/download) installed.

```bash
# Start the API server
cd src/FeatureFlagEngine.Api
dotnet run

# The server starts at http://localhost:5182
```

That's it. The database (SQLite) is created automatically on first run — no setup needed.

## How to Run the Tests

```bash
# From the root folder
dotnet test

# You should see: 81 passed, 0 failed
```

## Try It Out

Once the server is running, you can test with these commands (or use Postman/browser):

**Create a feature flag:**
```bash
curl -X POST http://localhost:5182/api/flags \
  -H "Content-Type: application/json" \
  -d '{"name":"dark-mode","isEnabled":false,"description":"Dark theme for the UI"}'
```

**Turn it on for one user:**
```bash
curl -X PUT http://localhost:5182/api/flags/dark-mode/users/alice \
  -H "Content-Type: application/json" \
  -d '{"isEnabled":true}'
```

**Check if it's on for that user:**
```bash
curl -X POST http://localhost:5182/api/flags/dark-mode/evaluate \
  -H "Content-Type: application/json" \
  -d '{"userId":"alice"}'

# → {"flagName":"dark-mode","isEnabled":true}
#   (Alice gets it ON because of her user override, even though the global default is OFF)
```

## All Available Endpoints

| What you want to do              | Method   | URL                                       |
|----------------------------------|----------|-------------------------------------------|
| Create a flag                    | `POST`   | `/api/flags`                              |
| List all flags                   | `GET`    | `/api/flags`                              |
| Get one flag                     | `GET`    | `/api/flags/{name}`                       |
| Update the global on/off state   | `PUT`    | `/api/flags/{name}`                       |
| Delete a flag                    | `DELETE` | `/api/flags/{name}`                       |
| Evaluate a flag for a context    | `POST`   | `/api/flags/{name}/evaluate`              |
| Set a user override              | `PUT`    | `/api/flags/{name}/users/{userId}`        |
| Remove a user override           | `DELETE` | `/api/flags/{name}/users/{userId}`        |
| Set a group override             | `PUT`    | `/api/flags/{name}/groups/{groupId}`      |
| Remove a group override          | `DELETE` | `/api/flags/{name}/groups/{groupId}`      |
| Set a region override            | `PUT`    | `/api/flags/{name}/regions/{regionId}`    |
| Remove a region override         | `DELETE` | `/api/flags/{name}/regions/{regionId}`    |

## Project Structure

```
FeatureFlagEngine/
├── src/
│   ├── FeatureFlagEngine.Core/            ← The brain. All business rules live here.
│   │   ├── Models/                        ← FeatureFlag, UserOverride, GroupOverride, RegionOverride
│   │   ├── Services/                      ← FeatureFlagService (evaluation + CRUD logic)
│   │   └── Interfaces/                    ← IFeatureFlagRepository (contract, not implementation)
│   │
│   ├── FeatureFlagEngine.Infrastructure/  ← The database layer.
│   │   ├── Data/                          ← EF Core DbContext
│   │   └── Repositories/                  ← SQLite repository + in-memory cache wrapper
│   │
│   └── FeatureFlagEngine.Api/             ← The HTTP layer. Thin wrapper, no business logic.
│       └── Program.cs                     ← REST endpoints, DI wiring
│
└── tests/
    ├── FeatureFlagEngine.Core.Tests/      ← 57 unit tests (model, service, evaluation logic)
    └── FeatureFlagEngine.Api.Tests/       ← 24 integration tests (full HTTP round-trips)
```

**Why this matters:** The Core project has zero dependencies on databases or web frameworks. You could plug it into a CLI, a desktop app, or a gRPC service without changing a single line of Core code.

## Assumptions I Made

- **Flag names are case-sensitive** — `"Dark-Mode"` and `"dark-mode"` are treated as two different flags.
- **One region per evaluation** — when checking if a flag is on, the caller provides a single region (e.g. `"eu-west"`), not a list.
- **Group order is caller-controlled** — if a user is in multiple groups, the first matching group in the list wins. The caller decides priority by ordering the array.
- **No login required** — the API is open. In a real system, you'd add authentication on top.

## Tradeoffs I Chose (and Why)

| What I did | Why |
|---|---|
| **Used SQLite instead of Postgres** | Zero setup — just run `dotnet run` and the database appears. The repository interface means swapping to Postgres later is a one-file change. |
| **Added an in-memory cache** | Feature flags get evaluated on every request in real systems. The `CachedFeatureFlagRepository` wraps the database repo and serves repeated reads from memory. It invalidates on every write — simple and safe over clever. |
| **Made `Evaluate()` a pure static method** | No database calls, no side effects. You pass in a flag object and get back true/false. This makes it trivially testable and fast. |
| **Used split queries for database reads** | Loading a flag with its user, group, and region overrides in one query causes a "cartesian explosion" (duplicate rows). Split queries run 3 small queries instead of 1 big messy one. |
| **Minimal API over Controllers** | 12 endpoints don't need the ceremony of controller classes. Each endpoint is 5–10 lines: parse request → call service → return HTTP status. |

## What I'd Do Next

**With another hour:**
- Add Swagger UI so you can explore the API in a browser
- Add pagination to the "list all flags" endpoint
- Add an audit trail — log who changed what flag, when

**With another day:**
- Percentage-based rollouts — enable a feature for 10% of users, then 50%, then 100%
- Time-based rules — turn a feature on between two dates
- Swap SQLite for Postgres with connection pooling
- Add Redis as a distributed cache (so multiple API instances share the same cache)
- Containerise with Docker for one-command deployment

## Known Limitations

- **No protection against simultaneous edits** — if two people update the same flag at the exact same moment, one update could be lost. Production would use optimistic concurrency (row versioning).
- **Cache is per-server** — if you run multiple copies of the API, each has its own cache. They don't sync. Redis would fix this.
- **No bulk operations** — you can only create/update one flag at a time via the API.
- **SQLite doesn't handle heavy writes well** — it locks the whole database for writes. Fine for moderate traffic, but a real RDBMS (Postgres, SQL Server) would be needed at scale.
- **Deleted flags are gone forever** — there's no "undo" or soft-delete. In production, I'd add an `IsArchived` flag instead.
