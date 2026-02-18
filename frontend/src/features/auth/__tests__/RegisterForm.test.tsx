import { describe, it, expect } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { RegisterForm } from '../RegisterForm'
import { renderWithProviders, unauthenticatedState } from '@/test/test-utils'

describe('RegisterForm', () => {
  it('renders all form fields', () => {
    renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    expect(screen.getByLabelText(/username/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/^password$/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/confirm password/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /sign up/i })).toBeInTheDocument()
  })

  it('validates empty username', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    await user.click(screen.getByRole('button', { name: /sign up/i }))
    expect(screen.getByText(/please enter a username/i)).toBeInTheDocument()
  })

  it('validates short password', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'newuser')
    await user.type(screen.getByLabelText(/^password$/i), '12345')
    await user.type(screen.getByLabelText(/confirm password/i), '12345')
    await user.click(screen.getByRole('button', { name: /sign up/i }))
    expect(screen.getByText(/at least 6 characters/i)).toBeInTheDocument()
  })

  it('validates password mismatch', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'newuser')
    await user.type(screen.getByLabelText(/^password$/i), 'password123')
    await user.type(screen.getByLabelText(/confirm password/i), 'different')
    await user.click(screen.getByRole('button', { name: /sign up/i }))
    expect(screen.getByText(/passwords do not match/i)).toBeInTheDocument()
  })

  it('successful registration saves data to store', async () => {
    const user = userEvent.setup()
    const { store } = renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'newuser')
    await user.type(screen.getByLabelText(/^password$/i), 'password123')
    await user.type(screen.getByLabelText(/confirm password/i), 'password123')
    await user.click(screen.getByRole('button', { name: /sign up/i }))

    await waitFor(() => {
      expect(store.getState().auth.isAuthenticated).toBe(true)
    })
  })

  it('shows error when username already exists', async () => {
    const user = userEvent.setup()
    renderWithProviders(<RegisterForm />, { preloadedState: unauthenticatedState })
    await user.type(screen.getByLabelText(/username/i), 'existing')
    await user.type(screen.getByLabelText(/^password$/i), 'password123')
    await user.type(screen.getByLabelText(/confirm password/i), 'password123')
    await user.click(screen.getByRole('button', { name: /sign up/i }))

    await waitFor(() => {
      expect(screen.getByText(/already exists/i)).toBeInTheDocument()
    })
  })
})
