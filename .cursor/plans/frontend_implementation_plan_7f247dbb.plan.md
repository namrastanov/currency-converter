---
name: Frontend Implementation Plan
overview: "Update DEVELOPMENT_PLAN.md to match the actual Bearer token auth backend, then implement the full frontend (React 19 + Vite + Redux Toolkit + RTK Query + Tailwind + Shadcn UI) following Feature-Sliced Design across 3 phases: scaffolding, features, testing."
todos:
  - id: update-plan-auth
    content: "Update DEVELOPMENT_PLAN.md: replace all cookie/httpOnly references with Bearer token approach"
    status: completed
  - id: update-plan-endpoints
    content: "Update DEVELOPMENT_PLAN.md: remove /me and /logout endpoints, fix CORS and test sections"
    status: completed
  - id: scaffold-vite
    content: Scaffold Vite + React 19 + TypeScript project with all dependencies
    status: completed
  - id: setup-tailwind-shadcn
    content: Configure Tailwind CSS and install Shadcn UI components
    status: completed
  - id: setup-redux-rtk
    content: Set up Redux store, RTK Query baseApi with Bearer auth, typed hooks
    status: completed
  - id: auth-feature
    content: Implement authSlice, authApi, JWT decode helper, localStorage persistence
    status: completed
  - id: route-guards
    content: Implement ProtectedRoute, AdminRoute, and React Router config
    status: completed
  - id: layout-nav
    content: Build Header widget and Layout with navigation and role-based links
    status: completed
  - id: login-register
    content: Build Login and Register pages with forms, validation, and error handling
    status: completed
  - id: convert-page
    content: Build Currency Conversion page with dropdowns, validation, result display
    status: completed
  - id: rates-page
    content: Build Latest Rates page with currency selector, table, and skeleton loading
    status: completed
  - id: historical-page
    content: Build Historical Rates page with date picker, pagination, and page size selector
    status: completed
  - id: admin-page
    content: Build User Management page with role change, delete confirmation, and AdminRoute guard
    status: completed
  - id: frontend-tests
    content: Write Vitest + RTL + MSW tests for all features and route guards
    status: completed
---

# Frontend Implementation Plan

## Part A: DEVELOPMENT_PLAN.md Corrections (Bearer Token Alignment)

The actual backend differs from the plan in auth strategy. The following sections of [DEVELOPMENT_PLAN.md](DEVELOPMENT_PLAN.md) need to be updated to match the real code:

### A.1 Section 3.1 — AuthController Endpoints (lines 270-273)

**Current backend** (`AuthController.cs`):

- `POST /api/v1/auth/login` — returns `ApiResponse<AuthResult>` where `AuthResult(Token, Username, Role)` in response body, no cookie
- `POST /api/v1/auth/register` — same, returns 201 with `AuthResult` in body
- No `/auth/me` endpoint
- No `/auth/logout` endpoint

Replace lines 270-273 with Bearer token descriptions. Remove `/me` and `/logout` entirely.

### A.2 Section 3.2 — JWT Authentication (lines 282-284)

Remove all cookie-specific configuration:

- Remove `Set-Cookie` / httpOnly cookie delivery description
- Remove `JwtBearerEvents.OnMessageReceived` cookie extraction
- Remove `cookieName` from `JwtSettings` property list (actual: `Secret, Issuer, Audience, ExpirationMinutes`)
- State: JWT is returned in response body; frontend sends it via `Authorization: Bearer <token>` header

### A.3 Section 3.9 — CORS (line 371)

Remove `AllowCredentials()` requirement. The actual CORS configuration uses `WithOrigins() + AllowAnyHeader() + AllowAnyMethod()` — no credentials needed for Bearer token approach.

### A.4 Section 4.1 — Unit Tests (line 408)

Remove `/me returns user from claims` and `/logout clears auth cookie` references from AuthController test descriptions.

### A.5 Section 4.2 — Integration Tests (lines 420, 427-429)

- Line 420: change "no cookie / expired JWT" to "no Authorization header / expired JWT"
- Line 427: change to "Register endpoint creates user and returns AuthResult with token"
- Remove lines 428-429 (`/auth/me` and `/auth/logout` test scenarios)

### A.6 Section 5.2 — API Client Layer (lines 520-521)

- Replace `credentials: 'include'` with `prepareHeaders` that injects `Authorization: Bearer <token>` from Redux store
- Update `baseQueryWithReauth`: intercept 401 → clear token from Redux + localStorage → redirect to `/login`

### A.7 Section 5.3 — Authentication (lines 530-539)

Rewrite entirely for Bearer token approach:

- `authSlice` state: `{ token: string | null, user: { id, username, role } | null, isAuthenticated: boolean }`
- Token stored in Redux state + persisted to `localStorage` for page refresh survival
- Session rehydration on startup: read token from `localStorage`, decode JWT claims to extract user info, populate `authSlice`; if token expired → clear state, redirect to login
- Login/Register: store returned `AuthResult.token` in Redux + `localStorage`, extract user info from response
- Logout: clear `localStorage` token + dispatch `clearAuth()` — no backend call needed (stateless JWT)
- Remove all `/me` and `/logout` API call references

### A.8 Section 6.5, 7.1, 8.4 — Scattered Cookie References

- Section 7.1 (line 610): Change "no token in frontend — httpOnly cookie set by backend" to "token stored in Redux + localStorage, sent via Authorization header"
- Section 8.4 (line 675): Replace httpOnly cookie assumption with Bearer token trade-off: "JWT stored in localStorage — persists across page reloads; trade-off: vulnerable to XSS (mitigated by CSP headers + input sanitization)"
- Section 8.4 (lines 680-681): Update future improvements — replace "Refresh tokens (rotate httpOnly cookie)" with "Refresh tokens (silent renewal before expiration)" and remove CSRF reference

---

## Part B: Frontend Implementation

### Phase 5 — Scaffolding

#### 5.1 Project Init

```
frontend/
├── src/
│   ├── app/              # store.ts, hooks.ts, router.tsx, providers.tsx
│   ├── pages/
│   │   ├── admin/        # UserManagementPage.tsx (+ __tests__/)
│   │   ├── convert/      # ConvertPage.tsx
│   │   ├── historical/   # HistoricalPage.tsx (+ __tests__/)
│   │   ├── login/        # LoginPage.tsx
│   │   ├── rates/        # RatesPage.tsx
│   │   └── register/     # RegisterPage.tsx
│   ├── widgets/
│   │   ├── header/       # Header.tsx
│   │   └── layout/       # Layout.tsx
│   ├── features/
│   │   ├── auth/         # authSlice.ts, authApi.ts, LoginForm.tsx, RegisterForm.tsx, ProtectedRoute.tsx, AdminRoute.tsx (+ __tests__/)
│   │   ├── conversion/   # conversionApi.ts, ConversionForm.tsx (+ __tests__/)
│   │   ├── historical/   # historicalApi.ts, Pagination.tsx
│   │   └── admin/        # adminApi.ts
│   ├── entities/
│   │   ├── currency/     # types.ts, currenciesApi.ts, CurrencySelector.tsx
│   │   ├── rate/         # types.ts, ratesApi.ts
│   │   └── user/         # types.ts
│   ├── shared/
│   │   ├── api/          # baseApi.ts, types.ts
│   │   ├── config/       # env.ts
│   │   ├── lib/          # constants.ts, jwt.ts, utils.ts (+ __tests__/)
│   │   └── ui/           # badge, button, card, dialog, ErrorBoundary, input, label, select, skeleton, table, tooltip
│   ├── test/             # mocks/handlers.ts, mocks/server.ts, setup.ts, test-utils.tsx
│   ├── index.css
│   └── main.tsx
├── index.html
├── vite.config.ts
├── vitest.config.ts
├── tsconfig.json
├── tsconfig.app.json
├── tsconfig.node.json
├── eslint.config.js
└── package.json
```

Note: RTK Query API endpoints follow FSD principles — domain-specific queries (`currenciesApi`, `ratesApi`) are in `entities/`, while feature-specific endpoints (`authApi`, `conversionApi`, `historicalApi`, `adminApi`) are in `features/`. All inject into the shared `baseApi`.

Note: Tailwind CSS v4 configured via `@tailwindcss/vite` plugin (no separate `tailwind.config.js`). Shadcn UI components manually placed in `shared/ui/` (no `components.json`).

**Dependencies:** react 19, react-dom 19, react-router-dom 7, @reduxjs/toolkit, react-redux, react-hook-form, @hookform/resolvers, class-variance-authority, clsx, tailwind-merge, lucide-react, sonner, zod 4, date-fns

**Dev dependencies:** vite, @vitejs/plugin-react, typescript, @types/react, @types/react-dom, @types/node, @tailwindcss/vite, tailwindcss, eslint, @eslint/js, eslint-plugin-react-hooks, eslint-plugin-react-refresh, typescript-eslint, globals, vitest, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, msw, jsdom

#### 5.2 Core Infrastructure Files

1. **`shared/config/env.ts`** — export `API_BASE_URL` from `import.meta.env.VITE_API_URL` (default `http://localhost:5143/api/v1`)
2. **`shared/api/baseApi.ts`** — RTK Query `createApi` with `fetchBaseQuery`:

   - `baseUrl` from `API_BASE_URL`
   - `prepareHeaders`: read token from Redux state (`(getState() as RootState).auth.token`), inject `Authorization: Bearer <token>`
   - Wrap in `baseQueryWithReauth`: intercept 401 → dispatch `clearAuth()` + dynamically import router + `router.navigate('/login')`; intercept 429 → show Sonner `toast.error`
   - Tag types: `['Users', 'Currencies', 'Rates']`

3. **`shared/api/types.ts`** — `ApiResponse<T>` type, `ErrorResponse` type, `unwrapResponse()` helper for response envelope unwrapping
4. **`app/store.ts`** — `setupStore()` function with `combineReducers` (supports `preloadedState` for tests), exports `store`, `RootState`, `AppDispatch`
5. **`app/hooks.ts`** — typed `useAppDispatch`, `useAppSelector`
6. **`app/router.tsx`** — React Router v7 `createBrowserRouter` config — Login/Register outside Layout, protected pages inside `ProtectedRoute` + `Layout` wrapper, admin pages inside `AdminRoute`
7. **`app/providers.tsx`** — `<ErrorBoundary>` + `<Provider store={store}>` + `<RouterProvider>` + `<Toaster position="top-right" richColors />`

#### 5.3 Auth Layer

- **`features/auth/authSlice.ts`**:
  - State: `{ token, user: { id, username, role } | null, isAuthenticated }`
  - Actions: `setCredentials({ token, user })`, `clearAuth()`
  - On `setCredentials`: persist token to `localStorage` (using `TOKEN_KEY` constant)
  - On `clearAuth`: remove from `localStorage`
  - `loadInitialState()` function (called for `initialState`): attempt to read token from `localStorage`, call `isTokenExpired()` to check validity, if valid call `extractUserFromToken()` to decode user info; if expired → clear `localStorage`, return empty state
- **`features/auth/authApi.ts`** — RTK Query endpoints injected into `baseApi`:
  - `login` mutation: `POST /auth/login`
  - `register` mutation: `POST /auth/register`
  - `onQueryStarted` for both: on success, unwrap response → extract `token`, `username`, `role` → call `extractUserFromToken(token)` → dispatch `setCredentials({ token, user })`
- **`features/auth/LoginForm.tsx`** + **`features/auth/RegisterForm.tsx`** — form components with `react-hook-form` + `zodResolver` for validation, handle `isLoading` and `serverError` states
- **`shared/lib/jwt.ts`** — helper functions:
  - `decodeBase64Url(str)` — base64url to string
  - `decodeJwt(token)` — splits JWT, decodes payload, returns `JwtPayload` or null
  - `isTokenExpired(token)` — checks `exp` claim against `Date.now()`
  - `extractUserFromToken(token)` — extracts `AuthUser` from JWT claims, handles .NET `ClaimTypes` URIs (`http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name`, `http://schemas.microsoft.com/ws/2008/06/identity/claims/role`) with fallbacks to `name`/`role`

#### 5.4 Route Guards

- **`features/auth/ProtectedRoute.tsx`** — reads `isAuthenticated` and `token` from store, checks `isTokenExpired(token)`, redirects to `/login` if not authenticated or token expired
- **`features/auth/AdminRoute.tsx`** — additionally checks `user?.role === APP_ROLES.ADMIN`, redirects to `/` if not Admin

---

### Phase 6 — Features (6 pages)

#### 6.1 Layout and Navigation (`widgets/`)

- **`widgets/header/Header.tsx`** — app title with DollarSign icon, nav links with icons (Convert, Rates, Historical), conditional "Users" link for Admin, username + role display, Logout button (dispatches `clearAuth()` + navigates to `/login`)
- **`widgets/layout/Layout.tsx`** — Header + `<Outlet />` inside container div (no Toaster here — Toaster is in `app/providers.tsx`)

#### 6.2 Login Page (`pages/login/`)

- `LoginForm` with username/password fields
- Link to register page
- Error display for 401 / validation errors
- On success: redirect to `/convert`

#### 6.3 Register Page (`pages/register/`)

- `RegisterForm` with username/password fields + password confirmation (client-side only)
- Zod validation: username not empty (max 50), password min 6 / max 128
- On success: redirect to `/convert`

#### 6.4 Convert Page (`pages/convert/`)

- `ConversionForm` component with `react-hook-form` + `zodResolver` (Zod schema validates: from/to required, amount > 0, source ≠ target)
- `CurrencySelector` dropdowns populated from `GET /currencies` — restricted currencies shown as disabled with tooltip
- Uses `useLazyConvertQuery` from `conversionApi` — triggered on form submit
- Error display: both validation errors (per-field) and `serverError` (from API, parsed via `parseApiError`)
- Result display card: large converted amount, original-to-converted equation, rate + date info

#### 6.5 Latest Rates Page (`pages/rates/`)

- Base currency selector (`CurrencySelector` component) + manual refresh button (RefreshCw icon, spins during fetch)
- Uses `ratesApi` from `entities/rate/ratesApi.ts` (not in `features/`)
- Table of rates: sorted alphabetically by code, shows code + rate value (4-6 decimal places)
- Restricted currencies shown with reduced opacity and "restricted" badge
- Last updated date displayed in card description
- Loading skeleton for table (8 skeleton rows)

#### 6.6 Historical Rates Page (`pages/historical/`)

- Form with: base currency selector, date range picker (start, end) with native `<input type="date">`, "Search" button
- Client-side validation: start <= end, max 730 days (`MAX_HISTORICAL_RANGE_DAYS`), end <= today
- Sends `timezoneOffset: new Date().getTimezoneOffset()` with every query
- Search triggers state update (`searchParams`), query skips until search is executed
- Paginated table: Date, Currency Count, Sample Rate (USD or first available)
- Page size selector (10/25/50 from `PAGE_SIZES` constant) — resets page to 1 on change
- `Pagination` component with page controls (Prev/Next/page numbers), total count
- Empty state and skeleton loading

#### 6.7 User Management Page (`pages/admin/`)

- `AdminRoute` wrapper
- "Add User" button opens Create User dialog (username, password, role fields)
- Table: username (with "You" badge for current user, "Default Admin" badge), role, created date, actions
- Role change: native `<Select>` dropdown (User/Admin) — disabled for default admin (shown as text with tooltip)
- Delete: confirmation dialog — disabled for own account and default admin (with tooltip explaining why)
- `adminApi` endpoints: `getUsers`, `createUser`, `updateUserRole`, `deleteUser` — all with `'Users'` tag for cache invalidation
- Toast notifications (Sonner) for success/error on create, role change, and delete

#### 6.8 Shadcn UI Components Used

Manually placed in `shared/ui/`: `badge`, `button`, `card`, `dialog`, `input`, `label`, `select`, `skeleton`, `table`, `tooltip`, plus custom `ErrorBoundary` component

---

### Phase 7 — Testing

- **Vitest + React Testing Library + MSW**
- MSW handlers (`test/mocks/handlers.ts`) mock 9 API endpoints with realistic response shapes (missing: `POST /admin/users` create)
- Test setup: `test/setup.ts` (jsdom, MSW beforeAll/afterEach/afterAll), `test/test-utils.tsx` (custom `renderWithProviders` wrapping Redux store)
- Test files in `__tests__/` directories colocated with features/pages
- Key test scenarios:
  - `features/auth/__tests__/LoginForm.test.tsx`: submit with valid credentials, handle 401 error, display token in Redux
  - `features/auth/__tests__/RegisterForm.test.tsx`: submit, validation, handle conflict error
  - `features/auth/__tests__/authSlice.test.ts`: setCredentials / clearAuth / localStorage persistence
  - `features/auth/__tests__/ProtectedRoute.test.tsx`: redirect behavior for unauthenticated users
  - `features/auth/__tests__/AdminRoute.test.tsx`: redirect for non-admin users
  - `features/conversion/__tests__/ConversionForm.test.tsx`: renders, validates, displays conversion result
  - `pages/historical/__tests__/HistoricalPage.test.tsx`: paginated data display, date validation
  - `pages/admin/__tests__/UserManagementPage.test.tsx`: renders list, role change, delete confirmation
  - `shared/lib/__tests__/jwt.test.ts`: JWT decode, expiration check, user extraction

---

### API Contract Reference (actual backend responses)

```
Success: { data: T, errors: null, metadata: {...} | null }
Error:   { type, title, status, detail, errors: { field: [msgs] } | null }

POST /auth/login        body: { username, password }     → { data: { token, username, role } }
POST /auth/register     body: { username, password }     → 201 { data: { token, username, role } }
GET  /currencies                                         → { data: [{ code, name, isRestricted }] }
GET  /rates/latest?base=EUR                              → { data: { baseCurrency, date, rates } }
GET  /rates/historical?base=EUR&from=...&to=...&page&pageSize&timezoneOffset=-180 → { data: { baseCurrency, rates, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }, metadata: {...} }
GET  /convert?from=EUR&to=USD&amount=100                 → { data: { from, to, amount, result, rate, date } }
GET  /admin/users                                        → { data: [{ id, username, role, createdAt }] }
POST /admin/users            body: { username, password, role } → 201 { data: { id, username, role, createdAt } }
GET  /admin/users/{id}                                   → { data: { id, username, role, createdAt } }
PUT  /admin/users/{id}/role  body: { role }              → { data: { id, username, role, createdAt } }
DELETE /admin/users/{id}                                 → 204
```

Seeded admin credentials: `admin` / `admin123`