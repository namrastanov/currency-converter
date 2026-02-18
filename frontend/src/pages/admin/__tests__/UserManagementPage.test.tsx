import { describe, it, expect } from 'vitest'
import { screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { UserManagementPage } from '../UserManagementPage'
import { renderWithProviders, authenticatedState } from '@/test/test-utils'

describe('UserManagementPage', () => {
  it('displays user list', async () => {
    renderWithProviders(<UserManagementPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument()
      expect(screen.getByText('testuser')).toBeInTheDocument()
    })
  })

  it('shows "You" badge for current user', async () => {
    renderWithProviders(<UserManagementPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByText('You')).toBeInTheDocument()
    })
  })

  it('delete button is disabled for own account', async () => {
    renderWithProviders(<UserManagementPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByText('admin')).toBeInTheDocument()
    })

    const buttons = screen.getAllByRole('button') as HTMLButtonElement[]
    const disabledDeleteBtn = buttons.find(
      (btn) => btn.closest('tr')?.textContent?.includes('admin') && btn.disabled
    )
    expect(disabledDeleteBtn).toBeDefined()
  })

  it('shows delete confirmation dialog', async () => {
    const user = userEvent.setup()
    renderWithProviders(<UserManagementPage />, { preloadedState: authenticatedState })

    await waitFor(() => {
      expect(screen.getByText('testuser')).toBeInTheDocument()
    })

    const enabledDeleteButtons = (screen.getAllByRole('button') as HTMLButtonElement[]).filter(
      (btn) => !btn.disabled && btn.closest('tr')?.textContent?.includes('testuser')
    )
    expect(enabledDeleteButtons.length).toBeGreaterThan(0)
    await user.click(enabledDeleteButtons[0])

    await waitFor(() => {
      expect(screen.getByText(/this action cannot be undone/i)).toBeInTheDocument()
    })
  })
})
