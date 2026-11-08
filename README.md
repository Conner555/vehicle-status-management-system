# FleetFlow — Vehicle Rental & Operations Management System

> A full-stack vehicle rental and operations management project built with **ASP.NET Core Web API (.NET 10), SQL Server, Redis, and Vue 3**.
>
> **Current development milestone: V3.7 — Redis caching and performance evaluation.**

FleetFlow evolved from a vehicle status management application into a multi-role vehicle rental system. It supports multi-vehicle rental orders, payment simulation, vehicle handover and return, inspection scheduling, early-return refunds, operational reminders, and role-specific dashboards. The backend uses a layered **Controller → Service → Repository** architecture, with **ADO.NET / Microsoft.Data.SqlClient** for SQL Server access.

**Repository:** https://github.com/Conner555/vehicle-status-management-system

## Key Features

| Domain | Implemented capabilities |
| --- | --- |
| Identity & access | JWT Bearer authentication; User, Operator, Admin roles; role-based API authorization |
| Vehicles | CRUD, status history, search, pagination, model-based daily rates, time-window availability queries |
| Rentals | Multiple vehicles per order; item-level rental periods; order cancellation and expiry processing |
| Payment | MockWallet payment simulation; payment records; client-supplied idempotency keys |
| Operations | Operator handover, user return, operator task claim and inspection submission |
| Inspections | Scheduled simulated inspection time, return + 12-hour deadline, reminder and timeout tracking |
| Refunds | Cancellation refunds and eligible early-return refunds triggered after inspection completion |
| Admin portal | Dashboard, rental order list/details, payments and refunds display |
| Operator portal | Pending/claimed inspections, reminders, history and inspection-result submission |
| Caching (V3.7) | Redis Cache-Aside, invalidation, SingleFlight, negative caching, TTL jitter and fallback paths |

**Scope note:** Payments are simulated, not integrated with a real payment gateway. Inspection times are simulated for the demonstration workflow. Refund-failure compensation/retry and a complete maintenance workflow remain future work.

## Architecture

```text
Vue 3 / Vite / Element Plus / ECharts
                  |
            Axios + JWT
                  |
         ASP.NET Core Web API
                  |
        Authentication / RBAC
                  |
              Controllers
                  |
               Services
            /            \
     Redis cache       Repositories
     (read cache,            |
      invalidation)       SQL Server
                  |
         Hosted Background Services
       (order expiry / inspection reminders / timeouts)
```

The backend separates request validation, business rules and persistence. Multi-step updates such as rental/return operations use SQL Server transactions and concurrency controls. Cached Dashboard reads follow a Cache-Aside pattern; authoritative business state remains in SQL Server.

### Main Business Workflow

```text
Search available vehicles
  → Create order (one or more rental items)
  → Pay (MockWallet; idempotency key)
  → Operator handover
  → User returns vehicle
  → Inspection task created
  → Operator claims and completes inspection
  → Vehicle/order state transitions
  → Eligible early-return refund after inspection
```

Order cancellation before handover follows a separate refund path. Vehicle inspection failures and maintenance escalation are areas for further business-flow development.

### Inspection Timing

- `ReturnedAt` records the vehicle return time.
- `ScheduledInspectionAt` represents a generated simulation time within the allowed inspection window.
- `InspectionDeadlineAt` is **return time + 12 hours**.
- A reminder may be raised after the scheduled time; exceeding the 12-hour deadline is treated as an inspection timeout.
- Background services synchronize reminders and timeout records; Admin and Operator have separate query views and authorization behavior.

## Redis Caching (V3.7)

### Cache Strategies

| Strategy | Purpose | Application |
| --- | --- | --- |
| Cache-Aside | Reduce repeated read queries | `GET /api/dashboard/summary` |
| Write invalidation | Remove stale summary after successful vehicle changes | Vehicle create/update/delete paths |
| SingleFlight | Coalesce concurrent cache misses within one app process | Dashboard and vehicle negative-cache paths |
| Shared cache gate | Coordinate Dashboard refill and invalidation in one process | Dashboard query and invalidator |
| Negative caching | Reduce repeated lookups of nonexistent vehicle IDs | `GET /api/vehicles/{id}` |
| TTL jitter | Spread expiration of different negative-cache keys | Vehicle negative-cache entries |
| Redis failure fallback | Preserve core SQL reads when Redis is unavailable | Cache-dependent read paths |

**Dashboard example:** A cache MISS obtains a process-local semaphore, checks Redis again, loads SQL data only if still missing, writes Redis, then releases the semaphore. A later query may hit immediately or after waiting for the gate. Vehicle write operations invalidate the dashboard key after the database write succeeds.

**Negative caching:** A nonexistent vehicle ID is represented by a short-lived `NOT_FOUND` marker; it is not returned as a fabricated vehicle DTO. A bounded set of per-ID striped semaphores reduces repeated concurrent misses. Negative cache expiration uses randomized TTL values (configured during this milestone around **15–30 seconds**).

**Limits:** `SemaphoreSlim`/striped locks coordinate requests **within one application process**, not across independently deployed app instances. Cache-Aside is not strict SQL/Redis atomic consistency. A short stale-read window may remain between database commit and cache invalidation; Redis deletion failures also need monitoring. A controlled stale-refill race test should remain in the regression-test checklist until its result is explicitly verified.

### Local Redis Setup

Start Redis locally and verify connectivity:

```bash
redis-cli PING
# PONG
```

From `VehicleStatusSystem/src/VehicleStatusSystem`, store your local Redis connection in .NET User Secrets:

```bash
dotnet user-secrets set "ConnectionStrings:Redis" "localhost:6379"
```

The actual configuration key and registration must match the current `Program.cs` in your local branch. Do not commit connection secrets, JWTs, `.http` authentication values or local `secrets.json` files.

Useful diagnostics (confirm your configured instance prefix/key naming):

```bash
redis-cli TYPE FleetFlow:dashboard:summary:v1
redis-cli TTL FleetFlow:dashboard:summary:v1
redis-cli TTL FleetFlow:vehicle:not-found:v1:999999
```

`IDistributedCache` may store entries using Redis hashes; do not assume `GET` is always valid. Use `TYPE` first.

### Dashboard Cache Toggle (Development Benchmarking)

Local tests compared Redis-enabled Dashboard requests with direct SQL queries using a development-only `CacheSettings:DashboardEnabled` configuration switch. Keep benchmarking overrides out of production configuration; enable the normal caching behavior again after testing. Turning off the Redis server is **not** equivalent to a clean SQL-only baseline because connection failure and retry costs may dominate.

## Performance Evaluation — V3.7.6

**Method:** Local Ubuntu development machine; Python threaded HTTP benchmark against `GET /api/dashboard/summary`; **5,000 requests per run**, 10/50/100 client concurrency settings, **3 runs per setting per mode**. SQL-only mode bypassed Dashboard Redis; cached mode used a warmed Redis entry. The 18 runs comprise **90,000 successful requests, 0 client-reported failures**.

### Throughput

| Client concurrency | SQL direct avg RPS | Redis warm avg RPS | Redis relative difference |
| ---: | ---: | ---: | ---: |
| 10 | 2,662.58 | 2,588.78 | -2.77% |
| 50 | 2,619.35 | 2,668.88 | +1.89% |
| 100 | 2,602.99 | 2,620.63 | +0.68% |

### Response Latency (mean of the three run-level statistics)

| Concurrent clients | SQL mean | Redis mean | SQL P95 | Redis P95 | SQL P99 | Redis P99 |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 10 | 3.58 ms | 3.69 ms | 5.67 ms | 5.98 ms | 7.77 ms | 8.61 ms |
| 50 | 14.64 ms | 12.77 ms | 29.41 ms | 27.60 ms | 36.17 ms | 35.22 ms |
| 100 | 17.19 ms | 16.75 ms | 45.88 ms | 46.86 ms | 60.48 ms | 63.49 ms |

**Interpretation:** For a small Dashboard dataset (tens of vehicles), Redis did **not** show a consistent or large HTTP throughput advantage. At 50 concurrent clients it was slightly faster in these short local runs, but the difference is small and should not be presented as a statistically established improvement. Its architectural benefit is the potential to avoid repeated SQL queries for hot reads; a measured SQL fallback count and longer-running workload remain useful follow-up checks.

**Benchmark limitations:** Each 5,000-request run lasted only around two seconds; client threads and the server may share the same machine. The results are client-observed HTTP measurements, not isolated Redis/SQL operation timings. Percentiles in the table are averages of run-level percentile values rather than recomputed pooled percentiles. Configured client concurrency is a limit, not a directly measured server in-flight count. No measured database query-count reduction or production-scale benchmark is claimed.

For a simple repeatable run, set a valid token in your shell (never commit it):

```bash
read -rsp "Admin JWT: " FLEETFLOW_ADMIN_TOKEN; echo
export FLEETFLOW_ADMIN_TOKEN
python3 tests/VehicleStatusSystem.Tests/performance/benchmark_dashboard.py
```

Adjust the script's `TOTAL_REQUESTS` and `CONCURRENCY` to reproduce the chosen scenario. Run multiple repetitions and compare the same workload and logging configuration in both modes.

## Frontend

**Frontend stack:** Vue 3, Vite, Axios, Vue Router, Element Plus and ECharts.

- **Admin:** vehicle dashboard, rental order list and details (items, payment and refund records), vehicle management, inspection-related views.
- **Operator:** overview cards, pending inspections, personal in-progress tasks, inspection submission, open/escalated reminders and inspection history.
- **Shared:** JWT-authenticated login, role-aware route guards, pagination, loading/error states and API service encapsulation.

The User-facing rental web interface and Admin-to-Operator reminder-sending workflow are planned improvements; the current emphasis is backend correctness and operations tooling.

## Representative API Endpoints

```http
POST /api/auth/register
POST /api/auth/login

GET  /api/vehicles/paged?page=1&pageSize=10
GET  /api/vehicles/{id}
GET  /api/vehicles/availability?startAt=...&endAt=...
POST /api/vehicles
PUT  /api/vehicles/{id}
DELETE /api/vehicles/{id}

POST /api/rental-orders
POST /api/rental-orders/{orderId}/pay
POST /api/rental-orders/{orderId}/cancel

GET  /api/dashboard/summary
GET  /api/inspections/history?page=1&pageSize=10
GET  /api/admin/inspection-timeouts
GET  /api/operator/inspection-reminders
```

Additional item-level handover, return, inspection claim/completion and Admin detail endpoints are implemented in their respective controllers; consult the running OpenAPI/Swagger documentation or `VehicleStatusSystem.http` for exact current request paths and DTOs. Operations that change rental/payment states require appropriate JWT roles and may require an `Idempotency-Key` header.

## Tech Stack

- **Backend:** C#, .NET 10, ASP.NET Core Web API, ADO.NET / Microsoft.Data.SqlClient, JWT, dependency injection, `BackgroundService`.
- **Database and caching:** SQL Server, Redis, `IDistributedCache`, SQL transactions, process-local `SemaphoreSlim`.
- **Frontend:** Vue 3, Vite, Axios, Vue Router, Element Plus, ECharts.
- **Testing and development:** xUnit, Moq, `.http` API scenarios, Python HTTP benchmark, Git/GitHub, Ubuntu and VS Code.

## Project Layout

```text
vehicle-status-management-system/
├── VehicleStatusSystem/
│   ├── src/VehicleStatusSystem/
│   │   ├── Controllers/
│   │   ├── Services/
│   │   ├── Repositories/
│   │   ├── Interfaces/
│   │   ├── DTOs/
│   │   ├── Caching/
│   │   ├── BackgroundServices/
│   │   ├── Program.cs
│   │   └── VehicleStatusSystem.http
│   └── tests/VehicleStatusSystem.Tests/
│       └── performance/benchmark_dashboard.py
├── frontend/                       # Check local branch for exact app folder
├── database/                       # SQL scripts, if included in branch
└── README.md
```

> Directory layouts have evolved across V2–V3. Confirm the exact frontend path and any moved benchmark/scripts against your **local V3.7 branch** before publication; this diagram is a conceptual orientation, not a verified exhaustive repository tree.

## Running Locally

Prerequisites: .NET 10 SDK, SQL Server, Redis, supported Node.js/npm, and Python 3 for optional benchmarking.

1. Clone the repository and check out the appropriate development branch.
2. Configure SQL Server and run the database initialization/migration scripts required by that branch.
3. Store `ConnectionStrings:DefaultConnection`, `ConnectionStrings:Redis`, and JWT secrets in .NET User Secrets or environment variables.
4. Run the backend from the local project folder:

```bash
cd VehicleStatusSystem/src/VehicleStatusSystem
dotnet restore
dotnet build
dotnet run
```

5. Run tests from the matching solution/project directory:

```bash
dotnet test
```

6. In another terminal, change into the frontend application directory containing `package.json`, then:

```bash
npm ci
npm run dev
```

The development backend and frontend have been tested locally using ports **5129** and **5173** respectively; actual ports depend on launch settings. CORS and JWT configuration must be valid for local frontend-to-backend requests.

## Verification Status

**Functional evidence from local development:**

- Rental payment, item-level handover/return, inspection completion and eligible automatic early-return refunds have been exercised through HTTP and SQL checks.
- Operator task views, claim/complete actions, reminders, cross-account restrictions and history views have been exercised.
- Redis read/write, MISS/HIT, TTL, cache invalidation after vehicle updates and Redis-unavailable fallback have been observed.
- Dashboard SingleFlight and vehicle negative caching have been exercised through concurrent requests.
- SQL-only and warm-Redis performance comparison has been run (see above).

**Remaining verification:** controlled stale-cache-refill concurrency test, repository-wide regression suite after final changes, and stronger long-running benchmarks with server-side counters. Do not interpret observed behavior as a claim that these outstanding checks have passed.

## Roadmap

- [ ] Complete controlled stale-refill race regression test and final V3.7 regression review.
- [ ] Add durable refund failure compensation, retry and reconciliation.
- [ ] Build failed-inspection → maintenance → vehicle availability workflow.
- [ ] Improve Admin reminder assignment and User-facing rental workflow.
- [ ] Add Docker Compose, CI and deployment/observability workflows.
- [ ] Extend benchmarking with SQL query counters, resource metrics and longer load durations.

## Security Notes

- Never commit passwords, JWTs, real customer information, Redis credentials or local `secrets.json`.
- The `MockWallet` flow is for demonstrations, not real financial transactions.
- Development-only endpoints (for example `GET /api/dev/redis-test`) should be disabled or removed outside development.
- Do not expose the SQL-only benchmarking switch as a public runtime control.

## License

No license is asserted here. Add a `LICENSE` file and update this section if you choose a license for the repository.
