import { createApi, fetchBaseQuery, type BaseQueryFn, type FetchArgs, type FetchBaseQueryError } from '@reduxjs/toolkit/query/react'
import { API_BASE_URL } from '@/shared/config/env'
import { clearAuth } from '@/features/auth/authSlice'
import type { RootState } from '@/app/store'
import { toast } from 'sonner'

const rawBaseQuery = fetchBaseQuery({
  baseUrl: API_BASE_URL,
  prepareHeaders: (headers, { getState }) => {
    const token = (getState() as RootState).auth.token
    if (token) {
      headers.set('Authorization', `Bearer ${token}`)
    }
    return headers
  },
})

const baseQueryWithReauth: BaseQueryFn<string | FetchArgs, unknown, FetchBaseQueryError> = async (
  args,
  api,
  extraOptions
) => {
  const result = await rawBaseQuery(args, api, extraOptions)

  if (result.error) {
    if (result.error.status === 401) {
      api.dispatch(clearAuth())
      const { router } = await import('@/app/router')
      router.navigate('/login')
    }

    if (result.error.status === 429) {
      toast.error('Too many requests. Please try again later.')
    }
  }

  return result
}

export const baseApi = createApi({
  reducerPath: 'api',
  baseQuery: baseQueryWithReauth,
  tagTypes: ['Users', 'Currencies', 'Rates'],
  endpoints: () => ({}),
})
