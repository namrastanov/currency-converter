import { Navigate, Outlet } from 'react-router-dom'
import { useAppSelector } from '@/app/hooks'
import { APP_ROLES } from '@/shared/lib/constants'

export function AdminRoute() {
  const { isAuthenticated, user } = useAppSelector((state) => state.auth)

  if (!isAuthenticated) return <Navigate to="/login" replace />
  if (user?.role !== APP_ROLES.ADMIN) return <Navigate to="/" replace />

  return <Outlet />
}
