import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { Routes, Route } from 'react-router-dom'
import { AdminRoute } from '../AdminRoute'
import { renderWithProviders, authenticatedState, userState, unauthenticatedState } from '@/test/test-utils'

function AdminContent() {
  return <div>Admin Content</div>
}

function HomePage() {
  return <div>Home Page</div>
}

function LoginPage() {
  return <div>Login Page</div>
}

describe('AdminRoute', () => {
  it('shows content for admin', () => {
    renderWithProviders(
      <Routes>
        <Route element={<AdminRoute />}>
          <Route path="/" element={<AdminContent />} />
        </Route>
        <Route path="/login" element={<LoginPage />} />
      </Routes>,
      { preloadedState: authenticatedState }
    )
    expect(screen.getByText('Admin Content')).toBeInTheDocument()
  })

  it('redirects to / for regular user', () => {
    renderWithProviders(
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/admin" element={<AdminRoute />}>
          <Route index element={<AdminContent />} />
        </Route>
      </Routes>,
      { preloadedState: userState, route: '/admin' }
    )
    expect(screen.getByText('Home Page')).toBeInTheDocument()
  })

  it('redirects to login for unauthenticated user', () => {
    renderWithProviders(
      <Routes>
        <Route element={<AdminRoute />}>
          <Route path="/" element={<AdminContent />} />
        </Route>
        <Route path="/login" element={<LoginPage />} />
      </Routes>,
      { preloadedState: unauthenticatedState }
    )
    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })
})
