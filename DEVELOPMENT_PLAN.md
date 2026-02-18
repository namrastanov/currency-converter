# Currency Converter Platform — Development Plan

## Project Structure

```
currency-converter/
├── backend/                  # ASP.NET Core 8 Web API
│   ├── CurrencyConverter.sln
│   ├── src/
│   │   ├── CurrencyConverter.Domain/
│   │   ├── CurrencyConverter.Application/
│   │   ├── CurrencyConverter.Infrastructure/
│   │   └── CurrencyConverter.API/
│   └── tests/
│       ├── CurrencyConverter.UnitTests/
│       └── CurrencyConverter.IntegrationTests/
├── frontend/                 # React + TypeScript (Vite)
├── docker-compose.yml        # API + Frontend + Redis
├── .gitignore
└── README.md
```

## Architecture Decision: Clean Architecture

**Dependency flow:** Domain ← Application ← Infrastructure ← API

| Layer | Responsibility | Dependencies |
|---|---|---|
| Domain | Models (Currency, ExchangeRate, User), interfaces (ICurrencyProvider), business rules (excluded currencies), constants (AppRoles), custom exceptions (CurrencyNotSupportedException, InvalidCredentialsException, UserAlreadyExistsException) | None |
| Application | Use cases (currency + auth), DTO/query/command objects, validation (FluentValidation), port interfaces (ICacheService, ICurrencyProviderFactory, IUserRepository, IJwtTokenService, IPasswordHasher), settings (JwtSettings, CacheSettings) | Domain |
| Infrastructure | Frankfurter HTTP client, Redis cache implementation, resilience policies (Microsoft.Extensions.Http.Resilience), provider factory, auth implementations (JwtTokenService, BCryptPasswordHasher, InMemoryUserRepository) | Domain, Application |
| API | Thin controllers, middleware (JWT Bearer config, logging, correlation, exception handling), DI composition root | All layers |

---

## Phase 1 — Backend: Domain & Application Layers

### 1.1 Domain Layer

- Define core models: `ExchangeRate`, `ConversionResult`, `Currency`, `PaginatedResult<T>`, `User`
- `User` entity with properties: `Id` (Guid, init), `Username` (init), `PasswordHash` (init), `Role` (mutable via `ChangeRole()` method), `CreatedAt` (init)
- Define `ICurrencyProvider` interface with four operations: get currencies list, get latest rates, convert, get historical rates
- Each provider must expose a `ProviderName` property for factory resolution
- Create `CurrencyRestrictions` static class with frozen set of excluded currencies (TRY, PLN, THB, MXN)
  - This is a **business rule**, not infrastructure concern — it belongs in Domain
  - All four currencies exist in Frankfurter API; the exclusion is purely our business decision
- Create `AppRoles` constants class with `Admin` and `User` roles
- Create custom exceptions: `CurrencyNotSupportedException`, `ExternalApiException`, `InvalidCredentialsException`, `UserAlreadyExistsException`

### 1.2 Application Layer

- Define port interfaces:
  - `ICacheService` (Get/Set with TTL) — for currency rate caching
  - `ICurrencyProviderFactory` (resolve by name) — for currency provider resolution
  - `IUserRepository` — pure CRUD: `GetByUsername`, `GetById`, `GetAll`, `Create`, `UpdateRole`, `Delete` (operates on `Domain.Models.User`)
  - `IJwtTokenService` — `string GenerateToken(User user)`
  - `IPasswordHasher` — `string Hash(string password)`, `bool Verify(string password, string hash)` (extracted for SRP)
- Define `JwtSettings` configuration class in `Application/Settings/` — both Infrastructure (`JwtTokenService`) and API (`AddJwtBearer`) depend on Application, so both can access it
- Implement **ten use cases** as separate classes:
  - **GetCurrenciesUseCase** — fetches available currencies from provider (cached), marks restricted currencies in response
  - **GetLatestRatesUseCase** — validates base currency against restrictions, checks cache, calls provider, caches result
  - **ConvertCurrencyUseCase** — validates both source AND target currency, delegates to provider
  - **GetHistoricalRatesUseCase** — validates currency + date range, detects cached vs uncached date gaps via `ICacheService`, fetches only missing sub-ranges from provider, stores new data, merges cached + fresh results, applies pagination (Skip/Take)
  - **LoginUseCase** — validates credentials via `IUserRepository` + `IPasswordHasher`, generates JWT via `IJwtTokenService`, throws `InvalidCredentialsException` on failure
  - **RegisterUseCase** — checks username uniqueness, hashes password, creates user, generates JWT, throws `UserAlreadyExistsException` if username taken
  - **GetAllUsersUseCase** — returns all users mapped to `UserDto`
  - **GetUserByIdUseCase** — returns single user by ID or null
  - **UpdateUserRoleUseCase** — validates role against `AppRoles`, updates user role
  - **DeleteUserUseCase** — prevents self-deletion (business rule), deletes user by ID
- Create query/DTO/command objects for each use case input:
  - Currency: `GetLatestRatesQuery`, `ConvertCurrencyQuery`, `GetHistoricalRatesQuery`, `CurrencyDto`, `LatestRatesDto`, `ConversionResultDto`, `HistoricalRatesDto`
  - Auth: `LoginCommand`, `RegisterCommand`, `AuthResult`, `UserDto`, `ChangeRoleCommand`, `DeleteUserCommand`
- FluentValidation validators:
  - Currency codes: not empty, 3 chars, not in excluded list
  - Amount: greater than zero
  - Date range: start ≤ end, max span 2 years (730 days), end ≤ today
  - Pagination: page ≥ 1, pageSize between 1 and 100
  - `LoginCommandValidator` — username/password not empty
  - `RegisterCommandValidator` — username not empty (max 50 chars), password min 6 / max 128 chars

### Key Design Decisions

- **Pagination strategy:** Frankfurter API does not support pagination — it returns all dates in a range as one JSON response. Our backend uses range-aware caching: checks which dates are already in Redis, fetches only the missing sub-ranges from Frankfurter, merges everything, and paginates from the combined data using offset/limit. Historical data is immutable, so cached dates never expire.
- **Excluded currencies in /latest and /historical responses:** Filter them out from rate dictionaries, not just block as base currency. Document this as a design decision.
- **Use cases vs services:** Separate classes per operation (not one god-service) for testability and SRP.
- **Auth follows the same Clean Architecture pattern as currency operations:** Domain entities + Application use cases/interfaces + Infrastructure implementations + thin API controllers. Auth business logic (credential validation, self-deletion prevention, role validation) lives in use cases, not controllers.

---

## Phase 2 — Backend: Infrastructure Layer

### 2.1 Frankfurter Provider

- Implement `ICurrencyProvider` as `FrankfurterProvider`
- **Critical:** Frankfurter uses path-based date format: `GET /{startDate}..{endDate}?base=EUR` — NOT query parameters
- Currencies list: `GET /currencies` → `{ "EUR": "Euro", "USD": "US Dollar", ... }`
- Latest rates: `GET /latest?base={currency}`
- Conversion: `GET /latest?from={source}&to={target}&amount={amount}`
- Historical: `GET /{start}..{end}?base={currency}`
- Create internal DTOs (`FrankfurterLatestResponse`, `FrankfurterTimeSeriesResponse`) for deserialization — these must NOT leak into Domain/Application
- Map Frankfurter DTOs to Domain models inside the provider
- Configure via `FrankfurterOptions` (base URL, timeout) from appsettings

### 2.2 Currency Provider Factory

- Implement `ICurrencyProviderFactory`
- Accept `IEnumerable<ICurrencyProvider>` via DI — all registered providers
- Build a dictionary by `ProviderName`, resolve by name or fall back to default from configuration
- Extensible: adding a new provider = new class + one DI registration line

### 2.3 Redis Cache Service

#### 2.3.1 Latest Rates Cache

- Simple key-value caching via `IDistributedCache`
- Cache key: `rates:latest:{base}` → JSON of rate dictionary
- TTL: 30 minutes (data updates daily ~16:00 CET, but we want reasonable freshness)
- Serialize/deserialize with System.Text.Json

#### 2.3.2 Historical Rates Cache — Range-Aware Strategy

Instead of caching entire date ranges as monolithic blobs, use **per-date granular caching** with gap detection. This avoids redundant Frankfurter API calls when requested ranges partially overlap with previously cached data.

**Redis data structures (per base currency):**

1. **Per-date rate data** — individual string keys:
   - Key: `rates:historical:{base}:{YYYY-MM-DD}` → JSON of rate dictionary for that date
   - TTL: no expiration (historical data is immutable once the day has passed)

2. **Fetched dates tracker** — Redis Sorted Set:
   - Key: `rates:historical:fetched:{base}`
   - Members: date strings (`"2020-01-02"`, `"2020-01-03"`, ...)
   - Score: date as numeric YYYYMMDD (e.g. `20200102`) for efficient range queries
   - This set includes ALL calendar dates that have been queried — even weekends/holidays that returned no data from Frankfurter. This prevents re-querying dates that are known to have no rates.

**Read flow for `GetHistoricalRates(base=EUR, from=2020-01-01, to=2020-03-31)`:**

1. **Check coverage:** `ZRANGEBYSCORE rates:historical:fetched:EUR 20200101 20200331` → returns all previously fetched dates within the requested range
2. **Generate all calendar dates** in [Jan 1, Mar 31] = ~91 dates
3. **Compute gap:** requested dates minus already-fetched dates = unfetched dates
4. **Group gaps into contiguous sub-ranges** to minimize API calls (e.g. [Feb 1 → Feb 28] instead of 28 individual calls)
5. **Fetch only gaps** from Frankfurter: `GET /{gapStart}..{gapEnd}?base=EUR` for each contiguous gap
6. **Store new data:**
   - Individual date keys for dates that returned rates
   - Add ALL calendar dates in the gap (including weekends with no data) to the fetched sorted set — so they won't be re-queried
7. **Retrieve full range:** `MGET rates:historical:EUR:2020-01-02 rates:historical:EUR:2020-01-03 ...` for all dates that have data (from sorted set)
8. **Paginate** the combined result and return

**Example scenario:**

```
Request 1: EUR, 2020-01-01..2020-01-31
  → Nothing cached. Fetch full range from Frankfurter.
  → Store 22 date keys (business days) + 31 dates in fetched set.

Request 2: EUR, 2020-01-15..2020-02-15
  → Check fetched set: Jan 15–31 already fetched (17 dates).
  → Gap detected: Feb 1–15 (15 dates not in fetched set).
  → Fetch only 2020-02-01..2020-02-15 from Frankfurter.
  → Store ~10 new date keys + 15 new dates in fetched set.
  → Combine cached Jan 15–31 data + fresh Feb 1–15 data.
  → Paginate and return.

Request 3: EUR, 2020-01-01..2020-02-15
  → Check fetched set: everything already covered.
  → Zero Frankfurter API calls. Serve entirely from Redis.
```

**Gap merging optimization:**

When gap detection produces multiple separate gaps, sending N requests is not always optimal. If two gaps are separated by a small already-cached region, it is cheaper to merge them into a single request (re-fetching a few cached days) than to pay for an extra HTTP round-trip.

**Rule:** When two adjacent gaps are separated by ≤ N days of cached data, merge them into one request. Otherwise, keep them as separate requests. N is configured in `appsettings.json` under `CacheSettings:GapMergeThresholdDays` (default: 5).

```
Example 1 — MERGE (gap between gaps = 5 days ≤ threshold):

  Requested:  Jan 1 ————————————————————— Jan 31
  Cached:               Jan 11–15
  Gaps:       Jan 1–10        Jan 16–31
  Gap:              ^^^  5 days  ^^^

  → Send ONE request: Jan 1..Jan 31
  → Re-fetches 5 days we already have — acceptable trade-off vs extra HTTP call

Example 2 — SPLIT (gap between gaps = 25 days > threshold):

  Requested:  Jan 1 ————————————————————————————— Mar 31
  Cached:               Jan 11 ——————— Feb 5
  Gaps:       Jan 1–10                     Feb 6–Mar 31
  Gap:              ^^^    25 days    ^^^

  → Send TWO requests: Jan 1..Jan 10 and Feb 6..Mar 31
  → Avoids re-fetching 25 days of data we already have
```

**Algorithm:**
1. Detect all contiguous gaps from the fetched sorted set
2. Walk through gaps left to right; if the "bridge" (cached region between two consecutive gaps) is ≤ threshold days → merge the two gaps into one
3. Repeat until no more merges are possible
4. Send one Frankfurter request per final merged gap
5. Discard duplicates when storing (dates already in cache are simply overwritten — idempotent)

The merge threshold is configured via `appsettings.json` → `CacheSettings:GapMergeThresholdDays` (default: 5). Can be tuned per environment based on observed Frankfurter API latency vs payload size trade-off.

**Edge cases to handle:**
- Today's date: if range includes today, always re-fetch today's rates (they are unstable until ~16:00 CET). Exclude today from the fetched sorted set entirely — this guarantees today's data is always re-fetched on every request.
- Redis unavailability: graceful fallback — log warning, fetch entire range from Frankfurter, return without caching. Cache must never break the app.
- Single remaining gap after merging: trivial case — one request.
- All gaps merge into one: equivalent to fetching the full range — but only happens when cache is mostly empty, which is expected for first requests.

#### 2.3.3 Cache Interface

- `ICacheService` should expose both simple key-value operations and range-aware historical operations:
  - `GetAsync<T>(key)` / `SetAsync<T>(key, value, ttl)` — for latest rates
  - `GetCachedDatesAsync(baseCurrency, startDate, endDate)` — returns set of already-fetched dates
  - `StoreDateRatesAsync(baseCurrency, date, rates)` — stores a single date's rates
  - `MarkDatesAsFetchedAsync(baseCurrency, dates)` — adds dates to the fetched sorted set
  - `GetDateRatesBatchAsync(baseCurrency, dates)` — MGET for multiple date keys
- Implementation lives in Infrastructure; interface in Application

### 2.4 Resilience (Microsoft.Extensions.Http.Resilience)

- Use `Microsoft.Extensions.Http.Resilience` — the official .NET 8 resilience layer built on top of Polly v8 (no direct Polly dependency needed)
- Apply a **custom resilience pipeline** to the typed `HttpClient` for `FrankfurterProvider` via `AddResilienceHandler()` with explicitly configured strategies
- **Pipeline order (outermost to innermost):** Total request timeout → Retry → Circuit Breaker → Attempt timeout
- **Configuration:**
  - Retry: 3 attempts, **exponential backoff** with jitter (delays: ~200ms, ~400ms, ~800ms). Uses `Backoff.ExponentialWithJitter` to prevent thundering herd
  - Circuit Breaker: open after 5 failures within 30-second sampling window, break duration 30 seconds
  - Attempt timeout: 10 seconds per individual attempt
  - Total timeout: 45 seconds for the entire request including all retries
- **Why custom `AddResilienceHandler()` over `AddStandardResilienceHandler()`:**
  - Full control over exponential backoff parameters and jitter strategy
  - Explicit pipeline configuration — easier to reason about and test
  - Same benefits of `Microsoft.Extensions.Http.Resilience`: native telemetry via `Metering` and `ILoggerFactory`, integrates with Serilog automatically
  - Strongly typed options configurable per environment via `appsettings.json`
- When circuit is open: use case should return cached data if available, or return a 503 with a clear message
- Resilience events (retries, circuit state changes) are automatically logged — no manual `onRetry` delegates needed

### 2.5 Auth Implementations

- Create `Auth/` directory in Infrastructure with:
  - **`JwtTokenService`** — implements `IJwtTokenService`, uses `IOptions<JwtSettings>` from Application layer, generates JWT with claims (`sub`, `name`, `role`, `client_id`, `jti`)
  - **`BCryptPasswordHasher`** — implements `IPasswordHasher`, uses `BCrypt.Net-Next` for hashing and verification
  - **`InMemoryUserRepository`** — implements `IUserRepository`, uses `ConcurrentDictionary<Guid, User>` for thread-safe storage, pre-seeds admin user on construction
- `BCrypt.Net-Next` and `System.IdentityModel.Tokens.Jwt` packages live in Infrastructure.csproj (not API.csproj)

### 2.6 DI Registration

- Create `DependencyInjection.cs` extension method in Infrastructure that registers:
  - `FrankfurterProvider` as `ICurrencyProvider` (keyed/named)
  - `CurrencyProviderFactory` as `ICurrencyProviderFactory`
  - `RedisCacheService` as `ICacheService`
  - `BCryptPasswordHasher` as `IPasswordHasher` (Singleton)
  - `JwtTokenService` as `IJwtTokenService` (Singleton)
  - Typed `HttpClient` for Frankfurter with custom `AddResilienceHandler()` pipeline (exponential backoff + circuit breaker)
  - Redis connection (`AddStackExchangeRedisCache`)
- Separate `AddInMemoryUserRepository()` extension method registers `InMemoryUserRepository` as `IUserRepository` — called conditionally in `Program.cs` (dev-only guard)

---

## Phase 3 — Backend: API Layer

### 3.1 Controllers (API v1)

- `CurrenciesController` — `GET /api/v1/currencies` — returns list of available currencies with display names (sourced from Frankfurter `GET /currencies`), with excluded currencies marked as `restricted: true`
- `RatesController` — `GET /api/v1/rates/latest?base=EUR`; `GET /api/v1/rates/historical?base=EUR&from=2020-01-01&to=2020-01-31&page=1&pageSize=10`
- `ConversionController` — `GET /api/v1/convert?from=EUR&to=USD&amount=100`
- `AuthController` — `[AllowAnonymous]` for login/register, delegates to `LoginUseCase` / `RegisterUseCase`, validates input via FluentValidation (`LoginCommandValidator`, `RegisterCommandValidator`)
  - `POST /api/v1/auth/login` — calls `LoginUseCase`, returns `AuthResult(Token, Username, Role)` in response body
  - `POST /api/v1/auth/register` — calls `RegisterUseCase`, returns `AuthResult(Token, Username, Role)` in response body (201 Created)
- `UserManagementController` — `[Authorize(Roles = "Admin")]`, delegates to `GetAllUsersUseCase`, `GetUserByIdUseCase`, `UpdateUserRoleUseCase`, `DeleteUserUseCase`
  - Extracts current user ID from JWT claims, passes to `DeleteUserCommand` for self-deletion prevention
- Controllers must be thin — only map HTTP request to command/query object, call use case, return result. No business logic in controllers.
- Return consistent response envelope: `{ data, errors, metadata }`

### 3.2 JWT Authentication & RBAC

- Configure JWT Bearer authentication via `AddJwtAuthentication()` extension method in `ServiceCollectionExtensions`
- **Bearer token authentication:** JWT is returned in the response body on login/register; frontend stores it and sends via `Authorization: Bearer <token>` header on subsequent requests
- `JwtSettings` (Secret, Issuer, Audience, ExpirationMinutes) defined in `Application/Settings/` — shared by both Infrastructure (`JwtTokenService`) and API (`AddJwtBearer`)
- JWT settings loaded from `appsettings.json` per environment; secret intentionally omitted from base config (only in `appsettings.Development.json` or environment variables for production)
- Security check: application throws on startup if JWT secret is empty or default
- Token validation: validate issuer, audience, lifetime, signing key; `ClockSkew = TimeSpan.Zero`
- Define roles as constants in `Domain/Constants/AppRoles`: `Admin` (full access), `User` (standard access)
- Auth business logic lives in Application layer use cases (not controllers):
  - `LoginUseCase` — validates credentials via `IUserRepository` + `IPasswordHasher`, generates JWT via `IJwtTokenService`
  - `RegisterUseCase` — checks uniqueness, hashes password, creates user, generates token
  - `DeleteUserUseCase` — enforces self-deletion prevention rule
  - `UpdateUserRoleUseCase` — validates role against `AppRoles`
- Token contains claims: `sub` (user ID), `name`, `role` (using `ClaimTypes.Role` for `[Authorize(Roles)]` compatibility), `client_id`, `jti`
- Protect all currency endpoints with `[Authorize]`
- Apply `[Authorize(Roles = AppRoles.Admin)]` to `UserManagementController`
- `client_id` claim will be extracted in logging middleware for observability

### 3.3 API Rate Limiting (Redis-backed)

- Use Redis-backed rate limiting to ensure consistent limits across all horizontally scaled instances
- Use `RedisRateLimiting` package integrated with ASP.NET Core 8 `AddRateLimiter()` infrastructure
- Rate limit counter stored in Redis — all instances share the same counter per client
- **Policy:**
  - Fixed window: 120 requests per minute per authenticated client (keyed by `client_id` from JWT)
  - Default value configured in `appsettings.json`, overridable per environment
- Configured in `appsettings.json` under `RateLimiting:RequestsPerMinute` (default: 120), overridable per environment via `appsettings.{env}.json`
- Return `429 Too Many Requests` with `Retry-After` header
- Graceful degradation: if Redis is unavailable, fall back to in-memory rate limiting with a logged warning (rate limiting should not break the app entirely)

### 3.4 Structured Logging & Observability

- Serilog with structured logging (JSON sink for Prod, Console for Dev)
- **Request Logging Middleware** — log for every request:
  - Client IP (respect `X-Forwarded-For` via `UseForwardedHeaders`)
  - Client ID (extracted from JWT `client_id` claim)
  - HTTP method and endpoint path
  - Response status code
  - Response time (ms)
- **Correlation ID Middleware:**
  - Read `X-Correlation-ID` from incoming request or generate a new GUID
  - Push to Serilog `LogContext` so every log line includes it
  - Attach to outgoing Frankfurter HTTP requests via `DelegatingHandler`
  - Frankfurter won't echo it back, but our logs will correlate inbound request ↔ outbound call ↔ response
  - Return `X-Correlation-ID` in response headers for client traceability
- Enrichers: `RequestId`, `MachineName`, `Environment`

### 3.5 Global Exception Handling

- Exception handling middleware that catches:
  - `InvalidCredentialsException` → 401 Unauthorized
  - `UserAlreadyExistsException` → 409 Conflict
  - `CurrencyNotSupportedException` → 400 with message
  - `ValidationException` (FluentValidation) → 400 with field-level errors
  - `ExternalApiException` → 502 Bad Gateway
  - `BrokenCircuitException` / `TimeoutRejectedException` (resilience pipeline) → 503 Service Unavailable
  - Unhandled exceptions → 500 with generic message (no stack trace in Prod)
- All error responses use consistent JSON format: `{ type, title, status, detail, errors }`
- Log every exception with correlation ID

### 3.6 API Versioning

- URL-based versioning: `/api/v1/...`
- Use `Asp.Versioning.Mvc` package
- Configure Swagger (Swashbuckle) to show versioned endpoints
- Default version: 1.0

### 3.7 Multi-Environment Configuration

- `appsettings.json` — shared defaults
- `appsettings.Development.json` — verbose logging, relaxed rate limits, local Redis
- `appsettings.Testing.json` — test-specific settings, WireMock URLs
- `appsettings.Production.json` — minimal logging, strict rate limits, production Redis
- All secrets (JWT key, Redis connection) should reference environment variables in Prod config

### 3.8 Health Checks

- Use ASP.NET Core `AddHealthChecks()` to expose application health for orchestrators, load balancers, and Docker
- **Liveness:** `GET /health/live` — confirms the process is running and not deadlocked (no external dependency checks)
- **Readiness:** `GET /health/ready` — confirms all dependencies are reachable:
  - Redis connectivity (`AddRedis()` from `AspNetCore.HealthChecks.Redis`)
  - Frankfurter API reachable (custom health check — lightweight `GET /currencies` call with short timeout)
- Health check endpoints are **excluded** from JWT authentication (anonymous access)
- Health check endpoints are **excluded** from rate limiting
- Response format: use `AspNetCore.HealthChecks.UI.Client` for detailed JSON output in Dev, minimal output in Prod
- Docker Compose and Kubernetes use these endpoints for container health monitoring

### 3.9 CORS

- Configure CORS to allow frontend origin (configurable per environment)
- No `AllowCredentials()` needed — Bearer token is sent via `Authorization` header, not cookies; explicit origins are still specified for security
- Dev: allow `http://localhost:5173` (Vite default)
- Prod: allow only the deployed frontend domain

---

## Phase 4 — Backend: Testing

### 4.1 Unit Tests (target ≥ 90% coverage)

**Domain layer:**
- `CurrencyRestrictions` — all 4 currencies blocked, valid currencies pass, case-insensitive
- Model creation and properties (including `User` entity)
- Auth exception tests (`InvalidCredentialsException`, `UserAlreadyExistsException`)

**Application layer (use cases) — the bulk of tests:**
- `GetLatestRatesUseCase` — cache hit returns cached data; cache miss calls provider then caches; restricted base currency throws; provider failure propagates
- `ConvertCurrencyUseCase` — happy path; restricted source throws; restricted target throws; zero/negative amount rejected by validator
- `GetHistoricalRatesUseCase` — pagination math (total count, total pages, correct slice); empty result handling; date range validation; **gap detection logic** (partially cached range returns correct gaps); today's date re-fetch behavior; merging cached + fresh data in correct order
- `LoginUseCase` — valid credentials return `AuthResult`; unknown user throws `InvalidCredentialsException`; wrong password throws `InvalidCredentialsException`
- `RegisterUseCase` — successful registration returns `AuthResult`; existing username throws `UserAlreadyExistsException`
- `GetAllUsersUseCase` — returns all users mapped to DTOs
- `GetUserByIdUseCase` — returns user when found; returns null when not found
- `UpdateUserRoleUseCase` — valid role update returns DTO; invalid role throws `ArgumentException`; unknown user returns null
- `DeleteUserUseCase` — successful deletion returns true; self-deletion throws `InvalidOperationException`; unknown user returns false
- All validators — boundary values, missing fields, invalid formats (including `LoginCommandValidator`, `RegisterCommandValidator`)

**Infrastructure layer:**
- `FrankfurterProvider` — correct URL construction (especially `{start}..{end}` format); response mapping from DTO to domain model; HTTP error handling
- `CurrencyProviderFactory` — resolves known provider; throws for unknown; uses default when name is null
- `RedisCacheService` — serialization/deserialization roundtrip; TTL is set correctly for latest rates; graceful handling when Redis is unavailable; **gap detection tests** (fully cached → no gaps; partially cached → correct gaps returned; nothing cached → full range as gap); **gap merging tests** (bridge ≤ threshold → gaps merge into single range; bridge > threshold → gaps stay separate; multiple consecutive small bridges → all merge; single gap → no merging needed); fetched set correctly marks weekends; MGET returns correct subset; today's date excluded from fetched set
- `JwtTokenService` — generates valid JWT; token contains correct claims (sub, name, role, client_id); correct expiration
- `BCryptPasswordHasher` — hash produces non-empty result; verify returns true for correct password; verify returns false for wrong password; same input produces different hashes (unique salts)
- `InMemoryUserRepository` — pre-seeds admin user; CRUD operations (create, getByUsername case-insensitive, getById, getAll, updateRole, delete); thread-safe

**API layer:**
- Controller tests mock use cases (not services) — verify controllers are thin wrappers
- `AuthController` — delegates to `LoginUseCase`/`RegisterUseCase`, throws exceptions propagated from use cases, validates input via FluentValidation
- `UserManagementController` — delegates to use cases, extracts current user ID from claims
- Middleware tests: correlation ID generation, exception-to-status-code mapping (including `InvalidCredentialsException` → 401, `UserAlreadyExistsException` → 409), request logging captures correct fields

### 4.2 Integration Tests

- Use `WebApplicationFactory<Program>` for in-process API testing
- Use **WireMock.Net** to mock Frankfurter API responses — do NOT call real API in CI
- Test scenarios:
  - Full happy path: login → get rates → convert → get historical with pagination
  - Excluded currency returns 400 with correct message
  - Frankfurter down (WireMock returns 500) → resilience pipeline triggers circuit breaker → 503 response
  - Unauthorized request (no Authorization header / expired JWT) → 401
  - Rate limiting triggers after N requests → 429
  - Pagination metadata is correct (totalCount, totalPages, hasNext)
  - Currencies endpoint returns full list with restricted currencies marked
  - Health check `/health/ready` returns healthy when Redis + Frankfurter are up
  - Health check `/health/ready` returns degraded/unhealthy when Redis is down
  - User management: Admin can list/update/delete users; User role gets 403 on admin endpoints
  - Register endpoint creates user and returns AuthResult with token
- Redis in integration tests: use **Testcontainers** to spin up a real Redis instance per test run — ensures realistic cache behavior without mocking

### 4.3 Coverage Reports

- Use Coverlet for collecting coverage during `dotnet test`
- Use ReportGenerator to produce HTML/Cobertura reports
- Add a script or Makefile target: `dotnet test --collect:"XPlat Code Coverage"` + report generation
- Aim: ≥ 90% line coverage, meaningful branch coverage

---

## Phase 5 — Frontend: Scaffolding

### 5.1 Project Setup

- Vite + React 19 + TypeScript (strict mode)
- Tailwind CSS + Shadcn UI for components
- React Router v7 (library mode) for navigation
- **Redux Toolkit** for state management + **RTK Query** for data fetching and caching
- **Note:** This is a client-side rendered (CSR) SPA — React Server Components and Server Actions do not apply. React 19 features used in CSR context: improved error handling, `ref` as prop (no `forwardRef`), `use()` hook for context consumption, document metadata support (`<title>`, `<meta>` from components)
- Project structure following **Feature-Sliced Design (FSD)** methodology:

```
src/
├── app/
│   ├── store.ts              # configureStore
│   ├── hooks.ts              # typed useAppDispatch, useAppSelector
│   ├── router.tsx            # React Router v7 route configuration
│   └── providers.tsx         # App-level providers (Redux, Router)
├── pages/
│   ├── login/
│   │   └── LoginPage.tsx
│   ├── register/
│   │   └── RegisterPage.tsx
│   ├── convert/
│   │   └── ConvertPage.tsx
│   ├── rates/
│   │   └── RatesPage.tsx
│   ├── historical/
│   │   └── HistoricalPage.tsx
│   └── admin/
│       └── UserManagementPage.tsx
├── widgets/
│   ├── header/
│   │   └── Header.tsx
│   └── layout/
│       └── Layout.tsx
├── features/
│   ├── auth/
│   │   ├── authSlice.ts      # login state, user info
│   │   ├── authApi.ts        # RTK Query: login, register, me, logout
│   │   └── LoginForm.tsx
│   ├── conversion/
│   │   ├── conversionApi.ts  # RTK Query endpoint
│   │   └── ConversionForm.tsx
│   ├── historical/
│   │   ├── historicalApi.ts  # RTK Query endpoint
│   │   ├── DateRangePicker.tsx
│   │   └── Pagination.tsx
│   └── admin/
│       ├── adminApi.ts       # RTK Query: user CRUD (Admin only)
│       ├── adminSlice.ts
│       └── RoleChangeForm.tsx
├── entities/
│   ├── currency/
│   │   ├── currenciesApi.ts  # RTK Query: getCurrencies
│   │   ├── CurrencySelector.tsx
│   │   └── types.ts
│   ├── rate/
│   │   ├── ratesApi.ts       # RTK Query: getLatestRates
│   │   └── types.ts
│   └── user/
│       └── types.ts
├── shared/
│   ├── ui/                   # Shadcn UI components
│   ├── api/
│   │   └── baseApi.ts        # RTK Query createApi base configuration
│   ├── lib/
│   │   ├── constants.ts
│   │   └── utils.ts
│   └── config/
│       └── env.ts            # VITE_API_URL and other env vars
└── main.tsx
```

**FSD layer rules (top imports bottom, never reverse):** `app` → `pages` → `widgets` → `features` → `entities` → `shared`

### 5.2 API Client Layer (RTK Query)

- Base API defined via `createApi` with `fetchBaseQuery` — base URL from environment variable (`VITE_API_URL`)
- `fetchBaseQuery` configured with `prepareHeaders` that reads JWT token from Redux store and injects `Authorization: Bearer <token>` header on every request
- `baseQueryWithReauth` wrapper: intercept 401 responses → clear token from Redux + localStorage → redirect to `/login`; intercept 429 → show rate limit toast via Sonner
- **API slices (organized by FSD layers):**
  - `authApi` (features/auth) — `login`, `register` mutations
  - `conversionApi` (features/conversion) — `convert({ from, to, amount })` query (lazy, triggered on form submit)
  - `historicalApi` (features/historical) — `getHistoricalRates({ base, from, to, page, pageSize })` query
  - `adminApi` (features/admin) — `getUsers`, `updateUserRole`, `deleteUser` (injected only for Admin role)
  - `currenciesApi` (entities/currency) — `getCurrencies` query (cached, rarely invalidated)
  - `ratesApi` (entities/rate) — `getLatestRates(base)` query

### 5.3 Authentication (Redux + Bearer Token)

- JWT stored in Redux state + persisted to `localStorage` for page refresh survival
- `authSlice` manages auth state: `{ token: string | null, user: { id, username, role } | null, isAuthenticated: boolean }`
- **Session rehydration on app startup:** read token from `localStorage`, decode JWT claims (base64 decode) to extract user info, populate `authSlice`; if token is expired → clear state, redirect to login
- Login page + registration page — both dispatch `authApi` mutations; frontend stores returned `AuthResult.token` in Redux + `localStorage`, extracts user info from response
- Logout — clear `localStorage` token + dispatch `clearAuth()` — no backend call needed (stateless JWT)
- `ProtectedRoute` component — reads `isAuthenticated` from Redux store, redirects to `/login` if false
- `AdminRoute` component — additionally checks `user.role === "Admin"`, redirects to `/` if not Admin
- Auto-logout on 401 response (handled in `baseQueryWithReauth` — clears Redux state + localStorage, redirects to login)

---

## Phase 6 — Frontend: Features

### 6.1 Currency Conversion Page

- Form: amount input, source currency dropdown, target currency dropdown
- Dropdowns populated from `GET /api/v1/currencies` — shows currency code + display name, excluded currencies shown as disabled with tooltip
- Excluded currencies (TRY, PLN, THB, MXN) shown as disabled in dropdown with tooltip explaining why
- Client-side validation before submit: amount > 0, source ≠ target, not excluded
- Display result: converted amount, rate, date
- Loading spinner during API call
- Error display for 400/500 responses

### 6.2 Latest Rates Page

- Currency selector (dropdown) for base currency
- Table showing all rates for selected base
- Excluded currencies visually marked (e.g. grayed out row with icon)
- Auto-refresh option or manual refresh button
- Loading skeleton for table

### 6.3 Historical Rates Page

- Date range picker (start date, end date) with validation:
  - Start ≤ end
  - Range not exceeding 2 years (730 days)
  - End ≤ today
- Base currency selector
- Paginated table with page controls (Previous / Next / page numbers)
- Page size selector (10 / 25 / 50)
- Display total records count and current page info
- Loading state during data fetch
- Handle empty results (e.g. "No data for this range")
- **Race condition prevention:** RTK Query automatically cancels outdated requests when query arguments change (built-in AbortController management) — no manual cancellation logic needed

### 6.4 User Management Page (Admin Only)

- Accessible only to users with `Admin` role — `AdminRoute` wrapper
- Navigation shows "User Management" link only for Admin users
- Table listing all registered users: username, role, created date
- Actions per user:
  - Change role (dropdown: `User` / `Admin`) — calls `PUT /api/v1/admin/users/{id}/role`
  - Delete user — confirmation dialog → calls `DELETE /api/v1/admin/users/{id}`
- Cannot delete own account (UI validation + backend check)
- RTK Query `adminApi` handles data fetching with automatic cache invalidation on mutations

### 6.5 Error Handling & UX

- Global error boundary (catches React rendering errors) — use React 19 improved error reporting
- Toast notifications for API errors (Sonner — lightweight, accessible toast library)
- Inline form validation messages
- **Suspense-first loading strategy:**
  - Wrap data-dependent page sections in `<Suspense fallback={<Skeleton />}>` for declarative loading states
  - Use RTK Query loading states (`isLoading`, `isFetching`) within Suspense boundaries
  - Skeleton components for tables (rates, historical, users), spinners for action buttons
  - Eliminates repetitive `if (isLoading) return <Spinner />` patterns — loading is handled at the boundary level
- Responsive layout (mobile-friendly)

---

## Phase 7 — Frontend: Testing

### 7.1 Component & Integration Tests

- Vitest + React Testing Library
- Focus areas:
  - **ConversionForm:** renders correctly, validates excluded currencies, submits with correct params, displays result/error
  - **HistoricalTable:** renders paginated data, page navigation works, empty state shown
  - **LoginForm / RegisterForm:** submits credentials, handles error response, verifies token + user info stored in Redux + localStorage on success (token sent via Authorization header)
  - **UserManagementPage:** renders user list for Admin, role change works, delete with confirmation, non-Admin users cannot access
  - **ProtectedRoute / AdminRoute:** redirects unauthenticated users, blocks non-Admin from admin pages
- Mock API calls with MSW (Mock Service Worker) for realistic HTTP mocking
- Do NOT test Shadcn UI internals — test behavior, not implementation

---

## Phase 8 — DevOps & Documentation

### 8.1 Docker

- **Backend Dockerfile:** multi-stage build (SDK for build, ASP.NET runtime for run), expose port 8080
- **Frontend Dockerfile:** multi-stage (Node for build, Nginx for serve), copy built assets to Nginx
- Nginx config: SPA fallback (`try_files $uri /index.html`), proxy `/api` to backend

### 8.2 Docker Compose

```
services:
  backend    → port 8080, depends_on: redis
  frontend   → port 3000, depends_on: backend
  redis      → port 6379, persistent volume
```

- Environment variables for each service
- Health checks for backend and Redis
- Dev override file (`docker-compose.override.yml`) with volume mounts for hot reload

### 8.3 CI/CD Pipeline (GitHub Actions)

Provide a `.github/workflows/ci.yml` that demonstrates CI/CD readiness:

**Backend pipeline (`backend-ci`):**
1. **Trigger:** on push/PR to `main` — changes in `backend/` directory
2. **Build:** `dotnet restore` → `dotnet build` — verify compilation
3. **Test:** `dotnet test` with Coverlet code coverage collection — fail if coverage < 90%
4. **Coverage report:** upload Cobertura report as pipeline artifact; optionally post summary to PR

**Frontend pipeline (`frontend-ci`):**
1. **Trigger:** on push/PR to `main` — changes in `frontend/` directory
2. **Install:** `npm ci` — deterministic dependency install
3. **Lint:** `npm run lint` — ESLint + TypeScript type check
4. **Test:** `npm run test` — Vitest with coverage
5. **Build:** `npm run build` — verify production build succeeds

**Integration pipeline (`integration-ci`):**
1. **Trigger:** on push/PR to `main` — runs after both backend and frontend pipelines
2. **Services:** spin up Redis via `services:` block in GitHub Actions
3. **Run:** backend integration tests with WireMock + real Redis (Testcontainers)

### 8.4 README.md

- **Setup instructions:** prerequisites, how to run with Docker Compose, how to run locally without Docker
- **Architecture overview:** diagram showing layers, data flow, caching strategy
- **API documentation:** endpoint list with request/response examples
- **AI usage section** (mandatory):
  - Which tools were used and for what purpose
  - Specific examples of AI suggestions that were accepted, modified, or rejected
  - Design decisions that were validated manually (e.g. Frankfurter API format, pagination strategy, cache TTL)
- **Assumptions and trade-offs:**
  - In-memory user store (no database) — sufficient for this scope; documented as future improvement
  - Redis required (not optional) for distributed caching, rate limiting, and distributed locks
  - Max date range limit: 2 years (730 days) for historical queries
  - Excluded currencies filtered from all responses (design decision)
  - JWT stored in localStorage — persists across page reloads; trade-off: vulnerable to XSS (mitigated by CSP headers + input sanitization)
  - Gap merge threshold (5 days) — trade-off: fewer HTTP calls vs slightly more data transfer
- **Future improvements:**
  - Add more providers (Fixer.io, Open Exchange Rates)
  - Database for user management
  - Refresh tokens (silent renewal before expiration)
  - Migrate to httpOnly cookie-based JWT for enhanced XSS protection
  - OpenTelemetry distributed tracing
  - Kubernetes deployment manifests

---

## Risk Registry

| Risk | Impact | Mitigation |
|---|---|---|
| Frankfurter API URL format (`start..end` path) | Build fails silently — wrong data | Manually test against real API before writing provider |
| Pagination: Frankfurter returns all data at once | Memory spike on large ranges | Validate max date range (730 days); range-aware caching fetches only missing sub-ranges |
| Redis unavailability | All requests hit Frankfurter directly | Graceful fallback — log warning, serve from provider |
| Resilience misconfiguration | Wrong policy order or conflicting timeouts | Custom `AddResilienceHandler()` with explicitly defined order: Total Timeout → Retry → Circuit Breaker → Attempt Timeout |
| Rate limiting under Redis failure | No rate limiting if Redis is down | `RedisRateLimiting` falls back to in-memory rate limiting with logged warning |
| MemoryCache vs horizontal scaling | Inconsistent cache across instances | Using Redis (IDistributedCache) solves this |
| Concurrent cache miss (thundering herd) | N parallel requests to Frankfurter for same data | Implement cache-aside with Redis distributed lock (SET NX EX) — only first request fetches, others wait for cache |
| Gap detection complexity | Incorrect sub-range calculation or missed dates | Thorough unit tests for gap detection and gap merging; mark weekends/holidays as fetched to prevent re-querying |
| Over-splitting requests | N small HTTP calls slower than 1 larger call | Gap merging with configurable threshold (default 5 days); merge small bridges between gaps |
| Today's rates in historical range | Stale data cached permanently | Exclude today from "fetched" set; always re-fetch today's rates on each request |
| JWT secret in source code | Security vulnerability | Use environment variables; document in README |
| Client IP behind proxy | Logs show proxy IP, not real client | Configure `UseForwardedHeaders` middleware |
| Today's exchange rates are unstable | Users see different rates within the same day | Short cache TTL (30 min) for latest; document that rates update ~16:00 CET |
| Race conditions in React (rapid parameter changes) | UI shows stale data | RTK Query auto-cancels outdated requests when query args change (built-in AbortController) |

---

## Estimated Effort

| Phase | Scope | Estimate |
|---|---|---|
| Phase 1 | Domain + Application | 2–3 hours |
| Phase 2 | Infrastructure (provider, Redis, resilience) | 3–4 hours |
| Phase 3 | API layer (controllers, auth, middleware, logging) | 3–4 hours |
| Phase 4 | Backend testing (unit + integration + coverage) | 4–5 hours |
| Phase 5 | Frontend scaffolding + Redux Toolkit + auth | 3–4 hours |
| Phase 6 | Frontend features (4 pages + admin + UX) | 5–6 hours |
| Phase 7 | Frontend testing | 2–3 hours |
| Phase 8 | Docker + CI/CD + README | 3–4 hours |
| **Total** | | **~25–35 hours** |
