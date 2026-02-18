export type ApiResponse<T> = {
  data: T | null
  metadata: Record<string, unknown> | null
}

export type ErrorResponse = {
  type: string
  title: string
  status: number
  detail: string
  errors: Record<string, string[]> | null
}

export function unwrapResponse<T>(response: ApiResponse<T>): T {
  if (response.data === null || response.data === undefined) {
    throw new Error('API response data is null')
  }
  return response.data
}
