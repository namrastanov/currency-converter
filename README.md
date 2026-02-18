# Currency Converter

## Architecture

```
┌─────────────────────┐       ┌──────────────────────────────────────────┐
│                     │       │            ASP.NET Core API              │
│   React SPA         │       │                                         │
│   (Vite + Redux     │ HTTP  │  Controllers ──► UseCases ──► Domain    │
│    Toolkit)          ├──────►│       │                        │        │
│                     │       │       ▼                        ▼        │
│   Feature-Sliced    │◄──────┤  Middleware        Infrastructure       │
│   Design            │  JSON │  (Auth, Errors,         │              │
│                     │       │   CORS, Rate Limit)     │              │
└─────────────────────┘       └─────────────────────────┼──────────────┘
                                                        │
                                          ┌─────────────┼─────────────┐
                                          │             │             │
                                          ▼             ▼             ▼
                                    ┌──────────┐  ┌──────────┐  ┌──────────────┐
                                    │  Redis   │  │ InMemory │  │ Frankfurter  │
                                    │  Cache   │  │ UserStore│  │ API          │
                                    └──────────┘  └──────────┘  │ (Provider)   │
                                                                └──────────────┘
```

---

## Human-Resolved Problems

### Pagination

The provider doesn't support pagination.

**Solution:** Redis cache stores all responses, and subsequent pages are served from Redis. Redis is needed to allow adding more instances (horizontal scaling).

### API Request Results Caching Policy

**Solution:** Gap management.

- We do not retrieve the entire range if part of it is already in cache.
- At the same time, we do not split requests to the provider if it's not worth it.
  - **Example:** If we need to retrieve from January 1st to January 10th and January 4–5 are already cached, it's not worth sending 2 separate requests.
  - **However:** If we request from January to July and March is already cached, it is worth sending two requests (Jan–Feb and Apr–Jul) and taking March from the cache.

**In practice**, because of gap management we see these results:

| # | Request Range | Response Time |
|---|---------------|---------------|
| 1st | `from=2025-09-04 to=2026-02-01` | 363 ms |
| 2nd (larger range) | `from=2025-08-04 to=2026-02-18` | 277 ms |

### Timezone

The user selects a date in their timezone, but the provider works in a different one. At some points we can end up asking the provider to get data for the future.

**Solution:** Send the user's `timezoneOffset` to the backend and convert the "today" date to UTC±0 before sending the request to the provider.

---

## Additional Features

Since the task asks to provide user roles, I implemented an admin page with user management:

- To have at least a couple of endpoints available to admins only.
- To be able to manage users (sign-up also exists).

---

## Working with AI

Used Cursor with Claude Opus 4.6 model.

**Step 1 — Analysis (~1 hour)**
Copied the test task into Cursor to analyze requirements, figure out edge cases, potential challenges, and modern best practices.
As a result, got the overall [Development Plan](./DEVELOPMENT_PLAN.md). Reviewing and fixing issues in that plan.

**Step 2 — Back-end implementation (~1 hour)**
In a new session, asked the AI to create a back-end implementation plan (Cursor built-in plans — [backend implementation plan](./.cursor/plans/backend_implementation_plan_2597dd61.plan.md)) based on the development plan. Reviewed the plan, generated the code, then iterated on fixing generated issues and reviewing.

**Step 3 — Front-end implementation (~1 hour)**
Same process for the front-end ([frontend implementation plan](./.cursor/plans/frontend_implementation_plan_7f247dbb.plan.md)).

**Step 4 — Local running & polishing (~2 hours)**
Bug fixes, reviewing the code, fixing hallucinated code, etc.

---

## Notes

- **Redis** is overkill here, but it was added to allow horizontal scaling later.
- **Redux Toolkit** on the front-end is also overkill, but implemented as if more features will be added later.
- The attached link to Lovable is not available, so no attention was paid to UI/UX.
- Users are not saved to the database, but the `InMemory` implementation uses an interface, so it is prepared for refactoring later.
- JWT is stored in `localStorage` — a more secure approach would be `HttpOnly` cookies, but it's not critical for now.
