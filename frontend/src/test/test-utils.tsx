import { render, type RenderOptions } from '@testing-library/react'
import { Provider } from 'react-redux'
import { MemoryRouter } from 'react-router-dom'
import { setupStore } from '@/app/store'
import type { RootState } from '@/app/store'

type PreloadedState = Partial<RootState>

type ExtendedRenderOptions = RenderOptions & {
  preloadedState?: PreloadedState
  route?: string
}

// Valid JWT with exp: 9999999999 (far future), sub/claims for admin user
const VALID_TEST_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwYWIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImNsaWVudF9pZCI6IjEyMzQ1Njc4LTEyMzQtMTIzNC0xMjM0LTEyMzQ1Njc4OTBhYiIsImp0aSI6InRlc3Qtand0LWlkIiwiZXhwIjo5OTk5OTk5OTk5fQ.fake-signature'

// Valid JWT with exp: 9999999999, sub/claims for regular user
const USER_TEST_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIyMjIyMjIyMi0yMjIyLTIyMjItMjIyMi0yMjIyMjIyMjIyMjIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoidGVzdHVzZXIiLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJVc2VyIiwiZXhwIjo5OTk5OTk5OTk5fQ.fake-signature'

export function renderWithProviders(
  ui: React.ReactElement,
  { preloadedState, route = '/', ...renderOptions }: ExtendedRenderOptions = {}
) {
  const store = setupStore(preloadedState)
  function Wrapper({ children }: { children: React.ReactNode }) {
    return (
      <Provider store={store}>
        <MemoryRouter initialEntries={[route]}>
          {children}
        </MemoryRouter>
      </Provider>
    )
  }
  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) }
}

export const authenticatedState: PreloadedState = {
  auth: {
    token: VALID_TEST_TOKEN,
    user: { id: '12345678-1234-1234-1234-1234567890ab', username: 'admin', role: 'Admin' },
    isAuthenticated: true,
  },
}

export const userState: PreloadedState = {
  auth: {
    token: USER_TEST_TOKEN,
    user: { id: '22222222-2222-2222-2222-222222222222', username: 'testuser', role: 'User' },
    isAuthenticated: true,
  },
}

export const unauthenticatedState: PreloadedState = {
  auth: {
    token: null,
    user: null,
    isAuthenticated: false,
  },
}
