import { describe, it, expect, beforeEach } from 'vitest'
import authReducer, { setCredentials, clearAuth } from '../authSlice'
import { TOKEN_KEY } from '@/shared/lib/constants'

describe('authSlice', () => {
  beforeEach(() => {
    localStorage.clear()
  })

  it('initial state without token is unauthenticated', () => {
    const state = authReducer(undefined, { type: 'unknown' })
    expect(state.isAuthenticated).toBe(false)
    expect(state.token).toBeNull()
    expect(state.user).toBeNull()
  })

  it('setCredentials sets token and user', () => {
    const user = { id: '1', username: 'admin', role: 'Admin' }
    const state = authReducer(undefined, setCredentials({ token: 'test-token', user }))
    expect(state.isAuthenticated).toBe(true)
    expect(state.token).toBe('test-token')
    expect(state.user).toEqual(user)
  })

  it('setCredentials persists token to localStorage', () => {
    const user = { id: '1', username: 'admin', role: 'Admin' }
    authReducer(undefined, setCredentials({ token: 'persist-me', user }))
    expect(localStorage.getItem(TOKEN_KEY)).toBe('persist-me')
  })

  it('clearAuth clears state and localStorage', () => {
    const user = { id: '1', username: 'admin', role: 'Admin' }
    const authed = authReducer(undefined, setCredentials({ token: 'token', user }))
    const cleared = authReducer(authed, clearAuth())
    expect(cleared.isAuthenticated).toBe(false)
    expect(cleared.token).toBeNull()
    expect(cleared.user).toBeNull()
    expect(localStorage.getItem(TOKEN_KEY)).toBeNull()
  })
})
