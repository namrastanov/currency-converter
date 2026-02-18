import { describe, it, expect } from 'vitest'
import { screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { ConversionForm } from '../ConversionForm'
import { renderWithProviders, authenticatedState } from '@/test/test-utils'

describe('ConversionForm', () => {
  it('renders the conversion form', async () => {
    renderWithProviders(<ConversionForm />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByLabelText(/from/i)).toBeInTheDocument()
    })
    expect(screen.getByLabelText(/^to$/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/amount/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /convert/i })).toBeInTheDocument()
  })

  it('validates unselected currency', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ConversionForm />, { preloadedState: authenticatedState })
    await user.click(screen.getByRole('button', { name: /convert/i }))
    expect(screen.getByText(/please select a source currency/i)).toBeInTheDocument()
  })

  it('shows conversion result', async () => {
    const user = userEvent.setup()
    renderWithProviders(<ConversionForm />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByLabelText(/from/i)).toBeInTheDocument()
    })

    await user.selectOptions(screen.getByLabelText(/from/i), 'EUR')
    await user.selectOptions(screen.getByLabelText(/^to$/i), 'USD')
    await user.type(screen.getByLabelText(/amount/i), '100')
    await user.click(screen.getByRole('button', { name: /convert/i }))

    await waitFor(() => {
      const results = screen.getAllByText(/104/)
      expect(results.length).toBeGreaterThanOrEqual(1)
    })
  })

  it('shows restricted currencies as disabled', async () => {
    renderWithProviders(<ConversionForm />, { preloadedState: authenticatedState })

    await waitFor(() => {
      const fromSelect = screen.getByLabelText(/from/i)
      const tryOption = within(fromSelect).getByText(/TRY/)
      expect(tryOption).toBeDisabled()
    })
  })
})
