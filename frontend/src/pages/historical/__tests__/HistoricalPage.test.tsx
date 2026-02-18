import { describe, it, expect } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { HistoricalPage } from '../HistoricalPage'
import { renderWithProviders, authenticatedState } from '@/test/test-utils'

describe('HistoricalPage', () => {
  it('renders the search form', async () => {
    renderWithProviders(<HistoricalPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByText(/historical rates/i)).toBeInTheDocument()
    })
    expect(screen.getByLabelText(/start date/i)).toBeInTheDocument()
    expect(screen.getByLabelText(/end date/i)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument()
  })

  it('shows data after search', async () => {
    const user = userEvent.setup()
    renderWithProviders(<HistoricalPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /search/i })).toBeInTheDocument()
    })
    await user.click(screen.getByRole('button', { name: /search/i }))

    await waitFor(() => {
      expect(screen.getByText(/total records/i)).toBeInTheDocument()
    })
  })

  it('validates date range', async () => {
    const user = userEvent.setup()
    renderWithProviders(<HistoricalPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByLabelText(/start date/i)).toBeInTheDocument()
    })

    const fromInput = screen.getByLabelText(/start date/i)
    const toInput = screen.getByLabelText(/end date/i)

    await user.clear(fromInput)
    await user.type(fromInput, '2025-03-01')
    await user.clear(toInput)
    await user.type(toInput, '2025-02-01')
    await user.click(screen.getByRole('button', { name: /search/i }))

    expect(screen.getByText(/start date cannot be later/i)).toBeInTheDocument()
  })
})
