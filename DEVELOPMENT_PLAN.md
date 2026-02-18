# Currency Converter Platform — Development Plan

## Project Structure

```
currency-converter/
├── backend/                  # ASP.NET Core 8 Web API
│   ├── CurrencyConverter.sln
│   ├── Dockerfile
│   ├── src/
│   │   ├── CurrencyConverter.Domain/
│   │   ├── CurrencyConverter.Application/
│   │   ├── CurrencyConverter.Infrastructure/
│   │   └── CurrencyConverter.API/
│   └── tests/
│       ├── CurrencyConverter.UnitTests/
│       └── CurrencyConverter.IntegrationTests/
├── frontend/                 # React + TypeScript (Vite)
│   └── Dockerfile
├── .github/workflows/        # CI pipelines
│   ├── backend-tests.yml
│   └── frontend-tests.yml
├── DEVELOPMENT_PLAN.md
├── .gitignore
└── README.md
```

## Architecture Decision: Clean Architecture

**Dependency flow:** Domain ← Application ← Infrastructure ← API

| Layer | Responsibility | Dependencies |
|---|---|---|
| Domain | Models (Currency, ExchangeRate, User, Result/Result&lt;T&gt;), interfaces (ICurrencyProvider), business rules (excluded currencies), constants (AppRoles), custom exceptions (CurrencyNotSupportedException, InvalidCredentialsException, UserAlreadyExistsException, ExternalApiException) | None |
| Application | Use cases (currency + auth + admin), DTO/query/command objects, validation (FluentValidation), port interfaces (ICacheService, ICurrencyProviderFactory, IUserRepository, IJwtTokenService, IPasswordHasher), settings (JwtSettings, CacheSettings, CurrencyProviderSettings) | Domain |
| Infrastructure | Frankfurter HTTP client, Redis cache implementation, resilience policies (Microsoft.Extensions.Http.Resilience), provider factory, auth implementations (JwtTokenService, BCryptPasswordHasher, InMemoryUserRepository) | Domain, Application |
| API | Thin controllers, middleware (JWT Bearer config, logging, correlation, exception handling), DI composition root | All layers |

---

## Phase 1 — Backend: Domain & Application Layers

### 1.1 Domain Layer

- Define core models: `ExchangeRate`, `ConversionResult`, `Currency`, `PaginatedResult<T>`, `User`, `Result` / `Result<T>`
- `User` entity with properties: `Id` (Guid, init), `Username` (init), `PasswordHash` (init), `Role` (mutable via `ChangeRole()` method), `CreatedAt` (init)
- `Result` / `Result<T>` — functional result pattern with `IsSuccess`, `Error`, `ErrorCode`, `IsFailure`; used by auth and admin use cases instead of throwing exceptions for business-logic failures
- Define `ICurrencyProvider` interface with four operations: get currencies list, get latest rates, convert, get historical rates
- Each provider must expose a `ProviderName` property for factory resolution
- Create `CurrencyRestrictions` static class with frozen set of excluded currencies (TRY, PLN, THB, MXN)
  - Exposes `IsRestricted(string)` and `GetExcludedCurrencies()` methods
  - This is a **business rule**, not infrastructure concern — it belongs in Domain
  - All four currencies exist in Frankfurter API; the exclusion is purely our business decision
- Create `AppRoles` constants class with `Admin`, `User` roles and `DefaultAdminUsername` constant
- Create custom exceptions: `CurrencyNotSupportedException`, `ExternalApiException`, `InvalidCredentialsException`, `UserAlreadyExistsException`

### 1.2 Application Layer

- Define port interfaces:
  - `ICacheService` (Get/Set with TTL + range-aware historical operations) — for currency rate caching
  - `ICurrencyProviderFactory` (resolve by name) — for currency provider resolution
  - `IUserRepository` — pure CRUD: `GetByUsername`, `GetById`, `GetAll`, `Create`, `TryCreate` (returns `(bool Created, User User)` tuple for atomic check-and-create), `UpdateRole`, `Delete` (operates on `Domain.Models.User`)
  - `IJwtTokenService` — `string GenerateToken(User user)`
  - `IPasswordHasher` — `string Hash(string password)`, `bool Verify(string password, string hash)` (extracted for SRP)
- Define settings classes in `Application/Settings/`:
  - `JwtSettings` (Secret, Issuer, Audience, ExpirationMinutes) — both Infrastructure (`JwtTokenService`) and API (`AddJwtBearer`) depend on Application, so both can access it
  - `CacheSettings` (LatestRatesTtlMinutes, CurrenciesTtlMinutes, GapMergeThresholdDays)
  - `CurrencyProviderSettings` (DefaultProvider)
- Implement **eleven use cases** as separate classes:
  - **GetCurrenciesUseCase** — fetches available currencies from provider (cached), marks restricted currencies in response
  - **GetLatestRatesUseCase** — validates base currency against restrictions, checks cache, calls provider, caches result
  - **ConvertCurrencyUseCase** — validates both source AND target currency, delegates to provider
  - **GetHistoricalRatesUseCase** — validates currency + date range, detects cached vs uncached date gaps via `ICacheService`, fetches only missing sub-ranges from provider, stores new data, merges cached + fresh results, applies pagination (Skip/Take). Uses `TimeProvider` for today's date detection. Accepts `TimezoneOffset` parameter to correctly determine "today" relative to the user's timezone
  - **LoginUseCase** — validates credentials via `IUserRepository` + `IPasswordHasher`, generates JWT via `IJwtTokenService`; returns `Result<AuthResult>` (Failure with `INVALID_CREDENTIALS` error code on invalid credentials — uses Result pattern instead of throwing exceptions)
  - **RegisterUseCase** — uses `IUserRepository.TryCreate` for atomic uniqueness check, hashes password, creates user with default `User` role, generates JWT; returns `Result<AuthResult>` (Failure with `USER_ALREADY_EXISTS` error code if username taken)
  - **CreateUserUseCase** — admin-only user creation with explicit role assignment; validates role against `AppRoles`, uses `TryCreate` for atomic uniqueness check; returns `Result<UserDto>`
  - **GetAllUsersUseCase** — synchronous (`Execute()`), returns all users mapped to `UserDto`
  - **GetUserByIdUseCase** — synchronous (`Execute(Guid id)`), returns single user by ID or null
  - **UpdateUserRoleUseCase** — synchronous, validates role against `AppRoles`, prevents changing default admin role; returns `Result<UserDto>`
  - **DeleteUserUseCase** — synchronous, prevents self-deletion and deletion of default admin account (business rules); returns `Result`
- Create query/DTO/command objects for each use case input:
  - Currency: `GetCurrenciesQuery`, `GetLatestRatesQuery`, `ConvertCurrencyQuery`, `GetHistoricalRatesQuery` (includes `TimezoneOffset`), `CurrencyDto`, `LatestRatesDto`, `ConversionResultDto`, `HistoricalRatesDto`
  - Auth: `LoginCommand`, `RegisterCommand`, `AuthResult`, `UserDto`, `CreateUserCommand` (Username, Password, Role), `ChangeRoleCommand` (UserId, NewRole), `DeleteUserCommand` (TargetUserId, CurrentUserId)
- FluentValidation validators:
  - Currency codes: not empty, 3 chars, not in excluded list
  - Amount: greater than zero
  - Date range: start ≤ end, max span 2 years (730 days), end ≤ today (adjusted by timezone offset)
  - Pagination: page ≥ 1, pageSize between 1 and 100
  - `LoginCommandValidator` — username/password not empty
  - `RegisterCommandValidator` — username not empty (max 50 chars), password min 6 / max 128 chars
  - `CreateUserCommandValidator` — username not empty (max 50 chars), password min 6 / max 128 chars, role must be Admin or User

### Key Design Decisions

- **Pagination strategy:** Frankfurter API does not support pagination — it returns all dates in a range as one JSON response. Our backend uses range-aware caching: checks which dates are already in Redis, fetches only the missing sub-ranges from Frankfurter, merges everything, and paginates from the combined data using offset/limit. Historical data is immutable, so cached dates never expire.
- **Excluded currencies in /latest and /historical responses:** Filter them out from rate dictionaries, not just block as base currency. Document this as a design decision.
- **Use cases vs services:** Separate classes per operation (not one god-service) for testability and SRP.
- **Result pattern for auth/admin use cases:** Instead of throwing exceptions for business-logic failures (invalid credentials, duplicate username, etc.), auth and admin use cases return `Result<T>` from Domain. This keeps exception handling for truly exceptional situations and provides typed error codes (`INVALID_CREDENTIALS`, `USER_ALREADY_EXISTS`, `SELF_DELETE`, `DEFAULT_ADMIN`, `NOT_FOUND`, `INVALID_ROLE`). Controllers map `Result.IsFailure` to appropriate HTTP status codes.
- **Auth follows the same Clean Architecture pattern as currency operations:** Domain entities + Application use cases/interfaces + Infrastructure implementations + thin API controllers. Auth business logic (credential validation, self-deletion prevention, default admin protection, role validation) lives in use cases, not controllers.

---

## Phase 2 — Backend: Infrastructure Layer

### 2.1 Frankfurter Provider

- Implement `ICurrencyProvider` as `FrankfurterProvider`
- **Critical:** Frankfurter uses `/v1/` prefix and path-based date format: `GET /v1/{startDate}..{endDate}?base=EUR` — NOT query parameters
- Currencies list: `GET /v1/currencies` → deserialized directly as `Dictionary<string, string>` (no dedicated currencies DTO)
- Latest rates: `GET /v1/latest?base={currency}`
- Conversion: `GET /v1/latest?from={source}&to={target}&amount={amount}`
- Historical: `GET /v1/{start:yyyy-MM-dd}..{end:yyyy-MM-dd}?base={currency}`
- Create internal DTOs (`FrankfurterLatestResponse`, `FrankfurterTimeSeriesResponse`) for deserialization — these must NOT leak into Domain/Application
- Map Frankfurter DTOs to Domain models inside the provider
- Configure via `FrankfurterOptions` (BaseUrl default `https://api.frankfurter.dev`, TimeoutSeconds) from appsettings

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
- Redis unavailability: graceful fallback — log warning, fetch entire range from Frankfurter, return without caching. Cache must never break the app. Error escalation: track consecutive failures (`_consecutiveErrors`), escalate log level to Error after threshold (5 consecutive failures).
- Single remaining gap after merging: trivial case — one request.
- All gaps merge into one: equivalent to fetching the full range — but only happens when cache is mostly empty, which is expected for first requests.

#### 2.3.3 Cache Interface

- `ICacheService` should expose both simple key-value operations and range-aware historical operations:
  - `GetAsync<T>(key)` / `SetAsync<T>(key, value, ttl)` — for latest rates
  - `GetCachedDatesAsync(baseCurrency, startDate, endDate)` — returns set of already-fetched dates
  - `StoreDateRatesAsync(baseCurrency, date, rates)` — stores a single date's rates
  - `MarkDatesAsFetchedAsync(baseCurrency, dates)` — adds dates to the fetched sorted set
  - `GetDateRatesBatchAsync(baseCurrency, dates)` — batch GET using Redis pipeline (`CreateBatch`) for multiple date keys
- Implementation (`RedisCacheService`) lives in Infrastructure; interface in Application
- `RedisCacheService` injects `IDistributedCache` (for simple key-value), `IConnectionMultiplexer` (for Sorted Set operations), `ILogger`, and `TimeProvider`

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
  - **`JwtTokenService`** — implements `IJwtTokenService`, uses `IOptions<JwtSettings>` + `TimeProvider` from Application layer, generates JWT with claims (`sub`, `ClaimTypes.Name`, `ClaimTypes.Role`, `client_id`, `jti`). Uses `TimeProvider` for token expiration calculation
  - **`BCryptPasswordHasher`** — implements `IPasswordHasher`, uses `BCrypt.Net-Next` for hashing and verification
  - **`InMemoryUserRepository`** — implements `IUserRepository`, uses `ConcurrentDictionary<Guid, User>` for thread-safe storage. Injects `IPasswordHasher` and `TimeProvider`. Pre-seeds admin user (`admin`/`admin123`) on construction. Uses a `lock` object for thread-safe `TryCreate` (check-and-create atomicity)
- `BCrypt.Net-Next` and `System.IdentityModel.Tokens.Jwt` packages live in Infrastructure.csproj (not API.csproj)

### 2.6 DI Registration

- Create `DependencyInjection.cs` extension method in Infrastructure that registers:
  - `FrankfurterOptions`, `CacheSettings`, `CurrencyProviderSettings` bound from configuration
  - `CorrelationIdDelegatingHandler` as Transient
  - Typed `HttpClient` for `ICurrencyProvider`/`FrankfurterProvider` with base URL, timeout, `CorrelationIdDelegatingHandler`, and custom `AddResilienceHandler()` pipeline (exponential backoff + circuit breaker)
  - `CurrencyProviderFactory` as `ICurrencyProviderFactory` (Transient)
  - `IConnectionMultiplexer` as Singleton (with `AbortOnConnectFail = false`)
  - `AddStackExchangeRedisCache` with instance name `CurrencyConverter:`
  - `RedisCacheService` as `ICacheService` (Singleton)
  - `BCryptPasswordHasher` as `IPasswordHasher` (Singleton)
  - `JwtTokenService` as `IJwtTokenService` (Singleton)
- Separate `AddInMemoryUserRepository()` extension method registers `InMemoryUserRepository` as `IUserRepository` (Singleton) — called in `Program.cs`

---

## Phase 3 — Backend: API Layer

### 3.1 Controllers (API v1)

- `CurrenciesController` — `GET /api/v1/currencies` — returns list of available currencies with display names (sourced from Frankfurter `GET /v1/currencies`), with excluded currencies marked as `restricted: true`
- `RatesController` — `GET /api/v1/rates/latest?base=EUR`; `GET /api/v1/rates/historical?base=EUR&from=2020-01-01&to=2020-01-31&page=1&pageSize=10&timezoneOffset=0`
- `ConversionController` — `GET /api/v1/convert?from=EUR&to=USD&amount=100`
- `AuthController` — `[AllowAnonymous]` for login/register, delegates to `LoginUseCase` / `RegisterUseCase`, validates input via FluentValidation. Maps `Result.IsFailure` to appropriate HTTP status codes (401 for `INVALID_CREDENTIALS`, 409 for `USER_ALREADY_EXISTS`)
  - `POST /api/v1/auth/login` — calls `LoginUseCase`, returns `ApiResponse<AuthResult>` with `AuthResult(Token, Username, Role)` in response body
  - `POST /api/v1/auth/register` — calls `RegisterUseCase`, returns `ApiResponse<AuthResult>` (201 Created)
- `UserManagementController` — `[Authorize(Roles = AppRoles.Admin)]`, delegates to `GetAllUsersUseCase`, `GetUserByIdUseCase`, `CreateUserUseCase`, `UpdateUserRoleUseCase`, `DeleteUserUseCase`
  - `POST /api/v1/admin/users` — admin-only user creation with explicit role assignment, validates via `CreateUserCommandValidator`
  - `GET /api/v1/admin/users` / `GET /api/v1/admin/users/{id}` — list/get users
  - `PUT /api/v1/admin/users/{id}/role` — change role (maps `Result` error codes to HTTP status codes)
  - `DELETE /api/v1/admin/users/{id}` — delete user, extracts current user ID from `ClaimTypes.NameIdentifier` for self-deletion prevention, also prevents deleting default admin
- Controllers must be thin — only map HTTP request to command/query object, call use case, map `Result` to HTTP response. No business logic in controllers.
- Success responses use `ApiResponse<T>` envelope: `{ data, metadata }` (no `errors` field in success responses)
- Error responses use `ErrorResponse` format: `{ type, title, status, detail, errors }` with RFC 7231 problem type URLs
- Additional API request models in `Models/AuthModels.cs`: `CreateUserRequest(Username, Password, Role)`, `ChangeRoleRequest(Role)`

### 3.2 JWT Authentication & RBAC

- Configure JWT Bearer authentication via `AddJwtAuthentication()` extension method in `ServiceCollectionExtensions`
- **Bearer token authentication:** JWT is returned in the response body on login/register; frontend stores it and sends via `Authorization: Bearer <token>` header on subsequent requests
- `JwtSettings` (Secret, Issuer, Audience, ExpirationMinutes) defined in `Application/Settings/` — shared by both Infrastructure (`JwtTokenService`) and API (`AddJwtBearer`)
- JWT settings loaded from `appsettings.json` per environment; secret intentionally set only in `appsettings.Development.json` (development) or environment variables (production)
- Security check: application throws on startup if JWT secret is empty/whitespace or equals the hardcoded `DefaultJwtSecret` constant
- Token validation: validate issuer, audience, lifetime, signing key; `ClockSkew = TimeSpan.Zero`
- Define roles as constants in `Domain/Constants/AppRoles`: `Admin` (full access), `User` (standard access)
- Auth business logic lives in Application layer use cases (not controllers):
  - `LoginUseCase` — validates credentials via `IUserRepository` + `IPasswordHasher`, generates JWT via `IJwtTokenService`; returns `Result<AuthResult>`
  - `RegisterUseCase` — checks uniqueness via `TryCreate`, hashes password, creates user, generates token; returns `Result<AuthResult>`
  - `CreateUserUseCase` — admin-only user creation with explicit role; returns `Result<UserDto>`
  - `DeleteUserUseCase` — enforces self-deletion prevention and default admin protection rules; returns `Result`
  - `UpdateUserRoleUseCase` — validates role against `AppRoles`, prevents changing default admin role; returns `Result<UserDto>`
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
- **Request Logging:** Uses Serilog's built-in `UseSerilogRequestLogging()` with custom `EnrichDiagnosticContext` delegate that enriches each request log with:
  - `ClientIP` (from `HttpContext.Connection.RemoteIpAddress`, respects `X-Forwarded-For` via `UseForwardedHeaders`)
  - `ClientId` (extracted from JWT `client_id` claim, defaults to `"anonymous"`)
- **Correlation ID Middleware:**
  - Read `X-Correlation-ID` from incoming request or generate a new GUID
  - Store in `HttpContext.Items["CorrelationId"]` and push to Serilog `LogContext` so every log line includes it
  - Attach to outgoing Frankfurter HTTP requests via `CorrelationIdDelegatingHandler`
  - Frankfurter won't echo it back, but our logs will correlate inbound request ↔ outbound call ↔ response
  - Return `X-Correlation-ID` in response headers for client traceability
- Enrichers: `ProcessId`, `MachineName`, `EnvironmentName` (via Serilog.Enrichers.Process and Serilog.Enrichers.Environment)

### 3.5 Global Exception Handling

- Exception handling middleware that catches:
  - `InvalidCredentialsException` → 401 Unauthorized
  - `UserAlreadyExistsException` → 409 Conflict
  - `CurrencyNotSupportedException` → 400 with message
  - `ValidationException` (FluentValidation) → 400 with field-level errors
  - `ExternalApiException` → 502 Bad Gateway
  - `BrokenCircuitException` / `TimeoutRejectedException` (resilience pipeline) → 503 Service Unavailable
  - Unhandled exceptions → 500 with generic message (no stack trace in Prod)
- All error responses use consistent `ErrorResponse` JSON format: `{ type, title, status, detail, errors }` with RFC 7231 problem type URLs
- Log every exception with correlation ID

### 3.6 API Versioning

- URL-based versioning: `/api/v1/...`
- Use `Asp.Versioning.Mvc` package
- Configure Swagger (Swashbuckle) to show versioned endpoints
- Default version: 1.0

### 3.7 Multi-Environment Configuration

- `appsettings.json` — shared defaults (Frankfurter options, cache settings, JWT issuer/audience/expiration, rate limiting, CORS, Redis connection string, Serilog)
- `appsettings.Development.json` — verbose logging, JWT secret (development-only), local Redis `localhost:6379`, `http://localhost:5173` CORS origin
- `appsettings.Testing.json` — test-specific settings, WireMock URLs, overridden JWT and Redis settings
- `appsettings.Production.json` — strict rate limits (60/min), minimal Serilog logging (Warning level), File sink. JWT secret loaded from environment variables (not in config file)
- All secrets (JWT key, Redis connection) should reference environment variables in Prod

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

- Configure CORS to allow frontend origin (configurable per environment via `CorsSettings:AllowedOrigins` array)
- No `AllowCredentials()` needed — Bearer token is sent via `Authorization` header, not cookies
- Uses `WithOrigins() + AllowAnyHeader() + AllowAnyMethod()` as default policy
- Dev: allow `http://localhost:5173` (Vite default)
- Prod: allow only the deployed frontend domain

---

## Phase 4 — Backend: Testing

### 4.1 Unit Tests (target ≥ 90% coverage)

**Domain layer:**
- `CurrencyRestrictionsTests` — all 4 currencies blocked, valid currencies pass, case-insensitive
- `ModelsTests` — model creation and properties (including `User` entity, `Result`/`Result<T>`)
- `ExceptionTests` — custom exception construction and properties (`CurrencyNotSupportedException`, `ExternalApiException`, `InvalidCredentialsException`, `UserAlreadyExistsException`)

**Application layer (use cases) — the bulk of tests:**
- `GetCurrenciesUseCaseTests` — cache hit/miss, restricted marking
- `GetLatestRatesUseCaseTests` — cache hit returns cached data; cache miss calls provider then caches; restricted base currency throws; restricted currencies filtered from rates
- `ConvertCurrencyUseCaseTests` — happy path; restricted source throws; restricted target throws; amount validated
- `GetHistoricalRatesUseCaseTests` — pagination math (total count, total pages, correct slice); empty result handling; **gap detection logic** (partially cached range returns correct gaps); today's date re-fetch behavior; merging cached + fresh data in correct order; gap merging with threshold
- `LoginUseCaseTests` — valid credentials return `Result.Success<AuthResult>`; unknown user returns `Result.Failure` with `INVALID_CREDENTIALS`; wrong password returns `Result.Failure`
- `RegisterUseCaseTests` — successful registration returns `Result.Success<AuthResult>`; existing username returns `Result.Failure` with `USER_ALREADY_EXISTS`
- `GetAllUsersUseCaseTests` — returns all users mapped to DTOs
- `GetUserByIdUseCaseTests` — returns user when found; returns null when not found
- `UpdateUserRoleUseCaseTests` — valid role update returns `Result.Success<UserDto>`; invalid role returns `Result.Failure`; unknown user returns `Result.Failure`; default admin role change prevented
- `DeleteUserUseCaseTests` — successful deletion returns `Result.Success`; self-deletion returns `Result.Failure` with `SELF_DELETE`; unknown user returns `Result.Failure` with `NOT_FOUND`; default admin deletion prevented
- `DtoTests` — DTO construction and property verification
- Validator tests — boundary values, missing fields, invalid formats for `GetLatestRatesQueryValidator`, `ConvertCurrencyQueryValidator`, `GetHistoricalRatesQueryValidator` (including timezone offset), `LoginCommandValidator`, `RegisterCommandValidator`, `CreateUserCommandValidator`

**Infrastructure layer:**
- `FrankfurterProviderTests` + `FrankfurterProviderAdditionalTests` — correct URL construction (especially `/v1/{start}..{end}` format); response mapping from DTO to domain model; HTTP error handling
- `CurrencyProviderFactoryTests` — resolves known provider; throws for unknown; uses default when name is null
- `RedisCacheServiceTests` + `RedisCacheServiceAdditionalTests` — serialization/deserialization roundtrip; TTL is set correctly for latest rates; graceful handling when Redis is unavailable; **gap detection tests** (fully cached → no gaps; partially cached → correct gaps returned; nothing cached → full range as gap); fetched set correctly marks weekends; batch GET returns correct subset; today's date excluded from fetched set; error escalation tracking
- `JwtTokenServiceTests` — generates valid JWT; token contains correct claims (sub, name, role, client_id, jti); correct expiration
- `BCryptPasswordHasherTests` — hash produces non-empty result; verify returns true for correct password; verify returns false for wrong password; same input produces different hashes (unique salts)
- `InMemoryUserRepositoryTests` — pre-seeds admin user; CRUD operations (create, TryCreate atomicity, getByUsername case-insensitive, getById, getAll, updateRole, delete); thread-safe
- `CorrelationIdDelegatingHandlerTests` — attaches correlation ID header to outgoing requests

**API layer:**
- Controller tests mock use cases (not services) — verify controllers are thin wrappers that map `Result` to HTTP responses
- `AuthControllerTests` — delegates to `LoginUseCase`/`RegisterUseCase`, maps `Result.IsFailure` to appropriate HTTP status codes (401/409), validates input via FluentValidation
- `UserManagementControllerTests` — delegates to use cases, extracts current user ID from `ClaimTypes.NameIdentifier`, maps error codes to HTTP status codes
- `CurrenciesControllerTests`, `RatesControllerTests`, `ConversionControllerTests` — thin wrapper verification
- `ApiResponseTests` — response envelope construction, `ErrorResponse` factory methods
- `ConfigurationTests` — DI configuration validation
- `FrankfurterHealthCheckTests` — health check behavior
- Middleware tests: `CorrelationIdMiddlewareTests` — correlation ID generation/propagation, `GlobalExceptionHandlingMiddlewareTests` + `GlobalExceptionHandlingMiddlewareAdditionalTests` — exception-to-status-code mapping (including `InvalidCredentialsException` → 401, `UserAlreadyExistsException` → 409, `ValidationException` → 400, `ExternalApiException` → 502, `BrokenCircuitException`/`TimeoutRejectedException` → 503)

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
- Tailwind CSS v4 (via `@tailwindcss/vite` plugin, no separate `tailwind.config.js`) + Shadcn UI components (manually placed, no `components.json`)
- React Router v7 (`react-router-dom`) for navigation
- **Redux Toolkit** for state management + **RTK Query** for data fetching and caching
- **react-hook-form** + `@hookform/resolvers` + **Zod** (v4) for form validation
- **Note:** This is a client-side rendered (CSR) SPA — React Server Components and Server Actions do not apply. React 19 features used in CSR context: improved error handling, `ref` as prop (no `forwardRef`), `use()` hook for context consumption, document metadata support (`<title>`, `<meta>` from components)
- Project structure following **Feature-Sliced Design (FSD)** methodology:

```
src/
├── app/
│   ├── store.ts              # setupStore() with combineReducers (supports preloadedState for tests)
│   ├── hooks.ts              # typed useAppDispatch, useAppSelector
│   ├── router.tsx            # React Router v7 createBrowserRouter route config
│   └── providers.tsx         # ErrorBoundary + Redux Provider + RouterProvider + Sonner Toaster
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
│   │   ├── authSlice.ts      # login state, user info, localStorage persistence
│   │   ├── authApi.ts        # RTK Query: login, register mutations
│   │   ├── LoginForm.tsx
│   │   ├── RegisterForm.tsx
│   │   ├── ProtectedRoute.tsx
│   │   └── AdminRoute.tsx
│   ├── conversion/
│   │   ├── conversionApi.ts  # RTK Query endpoint
│   │   └── ConversionForm.tsx
│   ├── historical/
│   │   ├── historicalApi.ts  # RTK Query endpoint (includes timezoneOffset)
│   │   └── Pagination.tsx
│   └── admin/
│       └── adminApi.ts       # RTK Query: getUsers, createUser, updateUserRole, deleteUser
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
│   ├── ui/                   # Shadcn UI components (badge, button, card, dialog, ErrorBoundary, input, label, select, skeleton, table, tooltip)
│   ├── api/
│   │   ├── baseApi.ts        # RTK Query createApi with Bearer auth + reauth wrapper
│   │   └── types.ts          # ApiResponse<T>, ErrorResponse, unwrapResponse helper
│   ├── lib/
│   │   ├── constants.ts      # APP_ROLES, TOKEN_KEY, RESTRICTED_CURRENCIES, PAGE_SIZES, MAX_HISTORICAL_RANGE_DAYS
│   │   ├── jwt.ts            # JWT decode helper (base64url decode, extract sub/name/role/exp claims)
│   │   └── utils.ts          # cn() for Tailwind, parseApiError() for error extraction
│   └── config/
│       └── env.ts            # VITE_API_URL (default: http://localhost:5143/api/v1)
├── test/
│   ├── mocks/
│   │   ├── handlers.ts       # MSW request handlers
│   │   └── server.ts         # MSW server setup
│   ├── setup.ts              # Vitest setup (jest-dom, MSW)
│   └── test-utils.tsx        # Custom render with Redux Provider
└── main.tsx
```

**FSD layer rules (top imports bottom, never reverse):** `app` → `pages` → `widgets` → `features` → `entities` → `shared`

### 5.2 API Client Layer (RTK Query)

- Base API defined via `createApi` with `fetchBaseQuery` — base URL from environment variable (`VITE_API_URL`, default `http://localhost:5143/api/v1`)
- `fetchBaseQuery` configured with `prepareHeaders` that reads JWT token from Redux state (`state.auth.token`) and injects `Authorization: Bearer <token>` header on every request
- `baseQueryWithReauth` wrapper: intercept 401 responses → dispatch `clearAuth()` + dynamically import router + navigate to `/login`; intercept 429 → show rate limit toast via Sonner
- Tag types: `['Users', 'Currencies', 'Rates']` for cache invalidation
- Shared `ApiResponse<T>` type and `unwrapResponse()` helper in `shared/api/types.ts` for response envelope unwrapping
- **API slices (organized by FSD layers, all injected into `baseApi` via `injectEndpoints`):**
  - `authApi` (features/auth) — `login`, `register` mutations; `onQueryStarted` dispatches `setCredentials` on success
  - `conversionApi` (features/conversion) — `convert({ from, to, amount })` query (lazy, triggered on form submit)
  - `historicalApi` (features/historical) — `getHistoricalRates({ base, from, to, page, pageSize, timezoneOffset })` query
  - `adminApi` (features/admin) — `getUsers`, `createUser`, `updateUserRole`, `deleteUser` (with `providesTags`/`invalidatesTags` for `'Users'` tag)
  - `currenciesApi` (entities/currency) — `getCurrencies` query (cached, rarely invalidated)
  - `ratesApi` (entities/rate) — `getLatestRates(base)` query

### 5.3 Authentication (Redux + Bearer Token)

- JWT stored in Redux state + persisted to `localStorage` (key from `TOKEN_KEY` constant) for page refresh survival
- `authSlice` manages auth state: `{ token: string | null, user: { id, username, role } | null, isAuthenticated: boolean }`
- **Session rehydration on app startup:** `loadInitialState()` function reads token from `localStorage`, calls `isTokenExpired()` to check validity, if valid calls `extractUserFromToken()` to decode JWT claims (base64url decode, handles .NET `ClaimTypes.Name` and `ClaimTypes.Role` claim URIs), populates `authSlice`; if token is expired → clears `localStorage`, returns empty state
- `shared/lib/jwt.ts` — helper functions: `decodeBase64Url`, `decodeJwt`, `isTokenExpired`, `extractUserFromToken` (parses `sub`, `http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`, `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` claims with fallbacks to `name`/`role`)
- Login page + registration page — both dispatch `authApi` mutations; `onQueryStarted` dispatches `setCredentials` with token + user info on success; `setCredentials` persists token to `localStorage`
- Logout — dispatch `clearAuth()` → removes from `localStorage` — no backend call needed (stateless JWT)
- `ProtectedRoute` component — reads `isAuthenticated` from Redux store and checks `isTokenExpired`, redirects to `/login` if not authenticated or token expired
- `AdminRoute` component — additionally checks `user?.role === APP_ROLES.ADMIN`, redirects to `/` if not Admin
- Login/Register pages are outside the `Layout` component (no header displayed). Protected pages are wrapped in `Layout` (Header + Outlet)
- Auto-logout on 401 response (handled in `baseQueryWithReauth` — dispatches `clearAuth()` to clear Redux state + localStorage, dynamically imports router + navigates to `/login`)

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
- Navigation shows "Users" link only for Admin users
- Create new user form: username, password, role selector — calls `POST /api/v1/admin/users`
- Table listing all registered users: username, role, created date
- Actions per user:
  - Change role (dropdown: `User` / `Admin`) — calls `PUT /api/v1/admin/users/{id}/role`
  - Delete user — confirmation dialog → calls `DELETE /api/v1/admin/users/{id}`
- Cannot delete own account (UI validation + backend check)
- RTK Query `adminApi` handles data fetching with `providesTags: ['Users']` and automatic cache invalidation via `invalidatesTags: ['Users']` on mutations

### 6.5 Error Handling & UX

- Global `ErrorBoundary` component (`shared/ui/ErrorBoundary.tsx`) wraps the entire app in `providers.tsx` — catches React rendering errors with fallback UI and "Try again" button
- Toast notifications for API errors and rate limiting (Sonner `<Toaster>` with `position="top-right" richColors`)
- Inline form validation messages via react-hook-form + Zod
- Loading states: RTK Query `isLoading`/`isFetching` states used for skeleton components (tables) and spinners (buttons)
- `parseApiError()` utility in `shared/lib/utils.ts` for consistent error message extraction from API responses
- Responsive layout (mobile-friendly with Tailwind CSS)

---

## Phase 7 — Frontend: Testing

### 7.1 Component & Integration Tests

- Vitest + React Testing Library + MSW (Mock Service Worker)
- MSW setup in `src/test/` with handlers, server, and custom `renderWithProviders` helper (wraps components in Redux Provider with optional preloaded state)
- Test files colocated with components in `__tests__/` directories
- Focus areas:
  - **ConversionForm:** renders correctly, validates excluded currencies, submits with correct params, displays result/error
  - **HistoricalPage:** renders paginated data, page navigation works, empty state shown
  - **LoginForm / RegisterForm:** submits credentials, handles error response, form validation with Zod
  - **UserManagementPage:** renders user list for Admin, role change works, delete with confirmation
  - **ProtectedRoute / AdminRoute:** redirects unauthenticated users, blocks non-Admin from admin pages
  - **authSlice:** `setCredentials` / `clearAuth` / `loadInitialState` / localStorage persistence
  - **jwt.ts:** JWT decode, token expiration check, user extraction from token claims
- Mock API calls with MSW for realistic HTTP mocking
- Do NOT test Shadcn UI internals — test behavior, not implementation

---

## Phase 8 — DevOps & Documentation

### 8.1 Docker

- **Backend Dockerfile** (`backend/Dockerfile`): multi-stage build — SDK 8.0 for restore + publish, ASP.NET 8.0 runtime for run. Sets `ASPNETCORE_ENVIRONMENT=Production`. Entrypoint: `dotnet CurrencyConverter.API.dll`
- **Frontend Dockerfile** (`frontend/Dockerfile`): multi-stage — Node 20 Alpine for install + build (accepts `VITE_API_URL` build arg), Nginx Alpine for serve. Inline Nginx config template with SPA fallback (`try_files $uri $uri/ /index.html`). Exposes port 80
- **Note:** No `docker-compose.yml` exists in the repository. Services are intended to be run individually or orchestrated externally

### 8.3 CI/CD Pipeline (GitHub Actions)

Separate workflow files in `.github/workflows/`:

**Backend pipeline** (`.github/workflows/backend-tests.yml` — "Backend Build & Test"):
1. **Trigger:** on `pull_request` to `main` — changes in `backend/**`
2. **Build job:** `dotnet restore` → `dotnet build --configuration Release` — verify compilation
3. **Test job** (depends on Build): `dotnet test` on unit tests project (`CurrencyConverter.UnitTests`) only — no coverage threshold enforcement in CI

**Frontend pipeline** (`.github/workflows/frontend-tests.yml` — "Frontend Build & Test"):
1. **Trigger:** on `pull_request` to `main` — changes in `frontend/**`
2. **Build job:** `npm ci` → `npm run build` — verify production build succeeds
3. **Test job** (depends on Build): `npm ci` → `npm test` — run Vitest unit tests

**Note:** No separate integration test pipeline exists in CI. Integration tests (with Testcontainers.Redis + WireMock.Net) are available locally but not automated in GitHub Actions

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
| Concurrent cache miss (thundering herd) | N parallel requests to Frankfurter for same data | Not explicitly mitigated with distributed locks in current implementation. Range-aware caching reduces the window, but concurrent first requests for the same uncached range may result in duplicate Frankfurter calls. Future improvement: add Redis distributed lock (SET NX EX) |
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
