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
│   ├── app/          # store, hooks, router, providers
│   ├── pages/        # 6 page components
│   ├── widgets/      # Header, Layout
│   ├── features/     # auth, conversion, historical, admin
│   ├── entities/     # currency, rate, user types
│   ├── shared/       # ui (shadcn), api, lib, config
│   └── main.tsx
├── index.html
├── vite.config.ts
├── tsconfig.json
├── tailwind.config.js
├── components.json   # shadcn config
└── package.json
```

**Dependencies:** react 19, react-dom 19, react-router 7, @reduxjs/toolkit, react-redux, tailwindcss, @radix-ui/*, class-variance-authority, clsx, tailwind-merge, lucide-react, sonner, zod, date-fns

**Dev dependencies:** vite, typescript, @types/react, @types/react-dom, @vitejs/plugin-react, eslint, vitest, @testing-library/react, @testing-library/jest-dom, @testing-library/user-event, msw, jsdom, postcss, autoprefixer

#### 5.2 Core Infrastructure Files

1. **`shared/config/env.ts`** — export `VITE_API_URL` (default `http://localhost:5080/api/v1`)
2. **`shared/api/baseApi.ts`** — RTK Query `createApi` with `fetchBaseQuery`:

   - `baseUrl` from env
   - `prepareHeaders`: read token from Redux state, inject `Authorization: Bearer <token>`
   - Wrap in `baseQueryWithReauth`: intercept 401 → `clearAuth()` + redirect; intercept 429 → show Sonner toast

3. **`app/store.ts`** — `configureStore` with RTK Query middleware and all API reducers + `authSlice`
4. **`app/hooks.ts`** — typed `useAppDispatch`, `useAppSelector`
5. **`app/router.tsx`** — React Router v7 route config (6 pages + `ProtectedRoute` + `AdminRoute`)
6. **`app/providers.tsx`** — `<Provider store={store}>` + `<RouterProvider>` + `<Toaster>` (Sonner)

#### 5.3 Auth Layer

- **`features/auth/authSlice.ts`**:
  - State: `{ token, user: { id, username, role } | null, isAuthenticated }`
  - Actions: `setCredentials(token, user)`, `clearAuth()`
  - On `setCredentials`: persist token to `localStorage`
  - On `clearAuth`: remove from `localStorage`
  - `initialState`: attempt to read token from `localStorage`, decode user info via JWT claims parsing (base64 decode, no crypto needed — just reading claims)
- **`features/auth/authApi.ts`** — RTK Query endpoints injected into `baseApi`:
  - `login` mutation: `POST /auth/login`
  - `register` mutation: `POST /auth/register`
  - `onQueryStarted` for both: on success, dispatch `setCredentials` with token + user
- **`features/auth/LoginForm.tsx`** + **`features/auth/RegisterForm.tsx`** — form components with Zod validation
- **`shared/lib/jwt.ts`** — helper to decode JWT payload (base64url decode, extract `sub`, `name`, `role`, `exp`)

#### 5.4 Route Guards

- **`features/auth/ProtectedRoute.tsx`** — reads `isAuthenticated` from store, redirects to `/login` if false
- **`features/auth/AdminRoute.tsx`** — additionally checks `user.role === "Admin"`, redirects to `/` if not

---

### Phase 6 — Features (6 pages)

#### 6.1 Layout and Navigation (`widgets/`)

- **`Header.tsx`** — app title, nav links (Convert, Rates, Historical), conditional "Users" link for Admin, username display, Logout button
- **`Layout.tsx`** — Header + `<Outlet />`, Sonner `<Toaster />`

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

- Form: amount input, source currency dropdown, target currency dropdown
- Dropdowns populated from `GET /currencies` — restricted currencies shown as disabled with tooltip
- Client-side validation: amount > 0, source != target, not restricted
- Result display: converted amount, rate, date
- Uses `conversionApi.convert` (lazy query, triggered on submit)

#### 6.5 Latest Rates Page (`pages/rates/`)

- Base currency dropdown selector
- Table of rates for selected currency (code + rate value)
- Restricted currencies visually marked (grayed row with icon)
- Manual refresh button
- Loading skeleton for table

#### 6.6 Historical Rates Page (`pages/historical/`)

- Date range picker (start, end) with validation: start <= end, max 730 days, end <= today
- Base currency selector
- Paginated table with page controls (Prev/Next/page numbers)
- Page size selector (10/25/50)
- Total records count + current page info
- Empty state handling

#### 6.7 User Management Page (`pages/admin/`)

- `AdminRoute` wrapper
- Table: username, role, created date
- Actions: Change role (dropdown User/Admin), Delete (confirmation dialog)
- Cannot delete own account (disable button + tooltip)
- RTK Query cache invalidation on mutations

#### 6.8 Shadcn UI Components Needed

Install via CLI: `button`, `input`, `label`, `select`, `table`, `card`, `dialog`, `dropdown-menu`, `badge`, `skeleton`, `tooltip`, `separator`, `form` (optional — or use native + Zod)

---

### Phase 7 — Testing

- **Vitest + React Testing Library + MSW**
- MSW handlers mock all 9 API endpoints with realistic response shapes
- Test files colocated next to components or in `__tests__/` directories
- Key test scenarios:
  - `LoginForm`: submit, error handling, token stored in Redux
  - `RegisterForm`: submit, validation, error handling
  - `ConversionForm`: renders, validates restricted currencies, displays result
  - `HistoricalTable`: paginated data, page navigation, empty state
  - `ProtectedRoute` / `AdminRoute`: redirect behavior
  - `UserManagementPage`: renders list, role change, delete confirmation
  - `authSlice`: setCredentials / clearAuth / localStorage persistence
  - `baseQueryWithReauth`: 401 interception clears auth

---

### API Contract Reference (actual backend responses)

```
Success: { data: T, errors: null, metadata: {...} | null }
Error:   { type, title, status, detail, errors: { field: [msgs] } | null }

POST /auth/login        body: { username, password }     → { data: { token, username, role } }
POST /auth/register     body: { username, password }     → 201 { data: { token, username, role } }
GET  /currencies                                         → { data: [{ code, name, isRestricted }] }
GET  /rates/latest?base=EUR                              → { data: { baseCurrency, date, rates } }
GET  /rates/historical?base=EUR&from=...&to=...&page&pageSize → { data: { baseCurrency, rates, totalCount, page, pageSize, totalPages, hasNextPage, hasPreviousPage }, metadata: {...} }
GET  /convert?from=EUR&to=USD&amount=100                 → { data: { from, to, amount, result, rate, date } }
GET  /admin/users                                        → { data: [{ id, username, role, createdAt }] }
GET  /admin/users/{id}                                   → { data: { id, username, role, createdAt } }
PUT  /admin/users/{id}/role  body: { role }              → { data: { id, username, role, createdAt } }
DELETE /admin/users/{id}                                 → 204
```

Seeded admin credentials: `admin` / `admin123`