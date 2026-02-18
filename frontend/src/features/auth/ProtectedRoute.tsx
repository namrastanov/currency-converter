import { useEffect } from 'react'
import { Navigate, Outlet } from 'react-router-dom'
import { useAppSelector, useAppDispatch } from '@/app/hooks'
import { clearAuth } from './authSlice'
import { isTokenExpired } from '@/shared/lib/jwt'

export function ProtectedRoute() {
  const dispatch = useAppDispatch()
  const { isAuthenticated, token } = useAppSelector((state) => state.auth)

  useEffect(() => {
    if (token && isTokenExpired(token)) {
      dispatch(clearAuth())
    }
  }, [token, dispatch])

  if (!isAuthenticated || (token && isTokenExpired(token))) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}
