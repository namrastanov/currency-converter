import { clsx, type ClassValue } from 'clsx'
import { twMerge } from 'tailwind-merge'

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}

export function parseApiError(error: unknown, fallback = 'An error occurred'): string {
  if (!error || typeof error !== 'object') return fallback
  const err = error as { data?: { detail?: string; errors?: Record<string, string[]> }; status?: number }
  if (err.data?.detail) return err.data.detail
  if (err.data?.errors) {
    const messages = Object.values(err.data.errors).flat()
    if (messages.length) return messages.join(', ')
  }
  return fallback
}