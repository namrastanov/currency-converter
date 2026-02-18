import { createSlice, type PayloadAction } from '@reduxjs/toolkit'
import type { AuthUser } from '@/entities/user/types'
import { TOKEN_KEY } from '@/shared/lib/constants'
import { extractUserFromToken, isTokenExpired } from '@/shared/lib/jwt'

type AuthState = {
  token: string | null
  user: AuthUser | null
  isAuthenticated: boolean
}

function loadInitialState(): AuthState {
  try {
    const token = localStorage.getItem(TOKEN_KEY)
    if (!token || isTokenExpired(token)) {
      localStorage.removeItem(TOKEN_KEY)
      return { token: null, user: null, isAuthenticated: false }
    }
    const user = extractUserFromToken(token)
    if (!user) {
      localStorage.removeItem(TOKEN_KEY)
      return { token: null, user: null, isAuthenticated: false }
    }
    return { token, user, isAuthenticated: true }
  } catch {
    return { token: null, user: null, isAuthenticated: false }
  }
}

const authSlice = createSlice({
  name: 'auth',
  initialState: loadInitialState(),
  reducers: {
    setCredentials(state, action: PayloadAction<{ token: string; user: AuthUser }>) {
      state.token = action.payload.token
      state.user = action.payload.user
      state.isAuthenticated = true
      localStorage.setItem(TOKEN_KEY, action.payload.token)
    },
    clearAuth(state) {
      state.token = null
      state.user = null
      state.isAuthenticated = false
      localStorage.removeItem(TOKEN_KEY)
    },
  },
})

export const { setCredentials, clearAuth } = authSlice.actions
export default authSlice.reducer
