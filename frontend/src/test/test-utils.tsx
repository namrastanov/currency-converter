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
    token: 'test-token',
    user: { id: '12345678-1234-1234-1234-1234567890ab', username: 'admin', role: 'Admin' },
    isAuthenticated: true,
  },
}

export const userState: PreloadedState = {
  auth: {
    token: 'test-token',
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
