import { describe, it, expect } from 'vitest'
import { screen } from '@testing-library/react'
import { Routes, Route } from 'react-router-dom'
import { ProtectedRoute } from '../ProtectedRoute'
import { renderWithProviders, authenticatedState, unauthenticatedState } from '@/test/test-utils'

function TestContent() {
  return <div>Protected Content</div>
}

function LoginPage() {
  return <div>Login Page</div>
}

describe('ProtectedRoute', () => {
  it('shows content for authenticated user', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<TestContent />} />
        </Route>
        <Route path="/login" element={<LoginPage />} />
      </Routes>,
      { preloadedState: authenticatedState }
    )
    expect(screen.getByText('Protected Content')).toBeInTheDocument()
  })

  it('redirects to login for unauthenticated user', () => {
    renderWithProviders(
      <Routes>
        <Route element={<ProtectedRoute />}>
          <Route path="/" element={<TestContent />} />
        </Route>
        <Route path="/login" element={<LoginPage />} />
      </Routes>,
      { preloadedState: unauthenticatedState }
    )
    expect(screen.getByText('Login Page')).toBeInTheDocument()
  })
})
