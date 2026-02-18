import { http, HttpResponse } from 'msw'
import { API_BASE_URL } from '@/shared/config/env'

const TEST_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwYWIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImNsaWVudF9pZCI6IjEyMzQ1Njc4LTEyMzQtMTIzNC0xMjM0LTEyMzQ1Njc4OTBhYiIsImp0aSI6InRlc3Qtand0LWlkIiwiZXhwIjo5OTk5OTk5OTk5fQ.fake-signature'

const TEST_USER_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyMjIyMjIyMi0yMjIyLTIyMjItMjIyMi0yMjIyMjIyMjIyMjIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidGVzdHVzZXIiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJVc2VyIiwiY2xpZW50X2lkIjoiMjIyMjIyMjItMjIyMi0yMjIyLTIyMjItMjIyMjIyMjIyMjIyIiwianRpIjoidGVzdC1qd3QtaWQtMiIsImV4cCI6OTk5OTk5OTk5OX0.fake-signature'

export const handlers = [
  http.post(`${API_BASE_URL}/auth/login`, async ({ request }) => {
    const body = await request.json() as { username: string; password: string }
    if (body.username === 'admin' && body.password === 'admin123') {
      return HttpResponse.json({
        data: { token: TEST_TOKEN, username: 'admin', role: 'Admin' },
        errors: null,
        metadata: null,
      })
    }
    return HttpResponse.json(
      { type: 'Unauthorized', title: 'Unauthorized', status: 401, detail: 'Invalid username or password.', errors: null },
      { status: 401 }
    )
  }),

  http.post(`${API_BASE_URL}/auth/register`, async ({ request }) => {
    const body = await request.json() as { username: string; password: string }
    if (body.username === 'existing') {
      return HttpResponse.json(
        { type: 'Conflict', title: 'Conflict', status: 409, detail: 'Username already exists.', errors: null },
        { status: 409 }
      )
    }
    return HttpResponse.json(
      {
        data: { token: TEST_USER_TOKEN, username: body.username, role: 'User' },
        errors: null,
        metadata: null,
      },
      { status: 201 }
    )
  }),

  http.get(`${API_BASE_URL}/currencies`, () => {
    return HttpResponse.json({
      data: [
        { code: 'EUR', name: 'Euro', isRestricted: false },
        { code: 'USD', name: 'US Dollar', isRestricted: false },
        { code: 'GBP', name: 'British Pound', isRestricted: false },
        { code: 'JPY', name: 'Japanese Yen', isRestricted: false },
        { code: 'TRY', name: 'Turkish Lira', isRestricted: true },
        { code: 'PLN', name: 'Polish Zloty', isRestricted: true },
      ],
      errors: null,
      metadata: null,
    })
  }),

  http.get(`${API_BASE_URL}/rates/latest`, ({ request }) => {
    const url = new URL(request.url)
    const base = url.searchParams.get('base') ?? 'EUR'
    return HttpResponse.json({
      data: {
        baseCurrency: base,
        date: '2025-02-17',
        rates: { USD: 1.0456, GBP: 0.8312, JPY: 157.23 },
      },
      errors: null,
      metadata: null,
    })
  }),

  http.get(`${API_BASE_URL}/convert`, ({ request }) => {
    const url = new URL(request.url)
    const from = url.searchParams.get('from') ?? 'EUR'
    const to = url.searchParams.get('to') ?? 'USD'
    const amount = parseFloat(url.searchParams.get('amount') ?? '100')
    const rate = 1.0456
    return HttpResponse.json({
      data: {
        from,
        to,
        amount,
        result: amount * rate,
        rate,
        date: '2025-02-17',
      },
      errors: null,
      metadata: null,
    })
  }),

  http.get(`${API_BASE_URL}/rates/historical`, ({ request }) => {
    const url = new URL(request.url)
    const base = url.searchParams.get('base') ?? 'EUR'
    const page = parseInt(url.searchParams.get('page') ?? '1')
    const pageSize = parseInt(url.searchParams.get('pageSize') ?? '10')
    return HttpResponse.json({
      data: {
        baseCurrency: base,
        rates: [
          { baseCurrency: base, date: '2025-02-14', rates: { USD: 1.0450, GBP: 0.8310 } },
          { baseCurrency: base, date: '2025-02-13', rates: { USD: 1.0440, GBP: 0.8300 } },
          { baseCurrency: base, date: '2025-02-12', rates: { USD: 1.0430, GBP: 0.8290 } },
        ],
        totalCount: 23,
        page,
        pageSize,
        totalPages: Math.ceil(23 / pageSize),
        hasNextPage: page < Math.ceil(23 / pageSize),
        hasPreviousPage: page > 1,
      },
      errors: null,
      metadata: { totalCount: 23, totalPages: Math.ceil(23 / pageSize), page, pageSize, hasNextPage: true, hasPreviousPage: false },
    })
  }),

  http.get(`${API_BASE_URL}/admin/users`, () => {
    return HttpResponse.json({
      data: [
        { id: '12345678-1234-1234-1234-1234567890ab', username: 'admin', role: 'Admin', createdAt: '2025-01-01T00:00:00Z' },
        { id: '22222222-2222-2222-2222-222222222222', username: 'testuser', role: 'User', createdAt: '2025-01-15T10:30:00Z' },
      ],
      errors: null,
      metadata: null,
    })
  }),

  http.put(`${API_BASE_URL}/admin/users/:id/role`, async ({ params, request }) => {
    const body = await request.json() as { role: string }
    return HttpResponse.json({
      data: { id: params.id, username: 'testuser', role: body.role, createdAt: '2025-01-15T10:30:00Z' },
      errors: null,
      metadata: null,
    })
  }),

  http.delete(`${API_BASE_URL}/admin/users/:id`, () => {
    return new HttpResponse(null, { status: 204 })
  }),
]
