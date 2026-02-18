import { Link, useNavigate } from 'react-router-dom'
import { useAppDispatch, useAppSelector } from '@/app/hooks'
import { clearAuth } from '@/features/auth/authSlice'
import { APP_ROLES } from '@/shared/lib/constants'
import { Button } from '@/shared/ui/button'
import { ArrowLeftRight, Clock, DollarSign, LogOut, Users } from 'lucide-react'

export function Header() {
  const dispatch = useAppDispatch()
  const navigate = useNavigate()
  const { user, isAuthenticated } = useAppSelector((state) => state.auth)

  function handleLogout() {
    dispatch(clearAuth())
    navigate('/login')
  }

  if (!isAuthenticated) return null

  return (
    <header className="border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container mx-auto flex h-14 items-center justify-between px-4">
        <div className="flex items-center gap-6">
          <Link to="/" className="flex items-center gap-2 font-semibold">
            <DollarSign className="h-5 w-5" />
            <span>Currency Converter</span>
          </Link>
          <nav className="flex items-center gap-4 text-sm">
            <Link to="/convert" className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground">
              <ArrowLeftRight className="h-4 w-4" />
              Convert
            </Link>
            <Link to="/rates" className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground">
              <DollarSign className="h-4 w-4" />
              Rates
            </Link>
            <Link to="/historical" className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground">
              <Clock className="h-4 w-4" />
              Historical
            </Link>
            {user?.role === APP_ROLES.ADMIN && (
              <Link to="/admin/users" className="flex items-center gap-1.5 text-muted-foreground transition-colors hover:text-foreground">
                <Users className="h-4 w-4" />
                Users
              </Link>
            )}
          </nav>
        </div>
        <div className="flex items-center gap-3">
          <span className="text-sm text-muted-foreground">
            {user?.username} ({user?.role})
          </span>
          <Button variant="ghost" size="sm" onClick={handleLogout}>
            <LogOut className="h-4 w-4" />
            Logout
          </Button>
        </div>
      </div>
    </header>
  )
}
