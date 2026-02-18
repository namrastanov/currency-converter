import { describe, it, expect } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { LoginForm } from '../LoginForm'
import { renderWithProviders, unauthenticatedState } from '@/test/test-utils'

describe('LoginForm', () => {
  it('renders input fields', () => {
    renderWithProviders(<LoginForm />, { preloadedState: unauthenticatedState })
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign in/i })).toBeInTheDocument()
  })

  it('shows error when username is empty', async () => {
    const user = userEvent.setup()
    renderWithProviders(<LoginForm />, { preloadedState: unauthenticatedState })
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    expect(screen.getByText(/please enter a username/i)).toBeInTheDocument()
  })

  it('shows error when password is empty', async () => {
    const user = userEvent.setup()
    renderWithProviders(<LoginForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'admin')
    await user.click(screen.getByRole('button', { name: /sign in/i }))
    expect(screen.getByText(/please enter a password/i)).toBeInTheDocument()
  })

  it('successful login saves data to store', async () => {
    const user = userEvent.setup()
    const { store } = renderWithProviders(<LoginForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'admin')
    await user.type(screen.getByLabelText(/password/i), 'admin123')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(store.getState().auth.isAuthenticated).toBe(true)
      expect(store.getState().auth.user?.username).toBe('admin')
    })
  })

  it('shows error on invalid credentials', async () => {
    const user = userEvent.setup()
    renderWithProviders(<LoginForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'wrong')
    await user.type(screen.getByLabelText(/password/i), 'wrong')
    await user.click(screen.getByRole('button', { name: /sign in/i }))

    await waitFor(() => {
      expect(screen.getByText(/invalid username or password/i)).toBeInTheDocument()
    })
  })
})
