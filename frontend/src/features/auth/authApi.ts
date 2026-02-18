import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { AuthResult } from '@/entities/user/types'
import { setCredentials } from './authSlice'
import { extractUserFromToken } from '@/shared/lib/jwt'

type LoginRequest = {
  username: string
  password: string
}

type RegisterRequest = {
  username: string
  password: string
}

export const authApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    login: builder.mutation<AuthResult, LoginRequest>({
      query: (body) => ({
        url: '/auth/login',
        method: 'POST',
        body,
      }),
      transformResponse: (response: ApiResponse<AuthResult>) => unwrapResponse(response),
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          const user = extractUserFromToken(data.token)
          if (user) {
            dispatch(setCredentials({ token: data.token, user }))
          }
        } catch {
          // errors handled by component
        }
      },
    }),

    register: builder.mutation<AuthResult, RegisterRequest>({
      query: (body) => ({
        url: '/auth/register',
        method: 'POST',
        body,
      }),
      transformResponse: (response: ApiResponse<AuthResult>) => unwrapResponse(response),
      async onQueryStarted(_arg, { dispatch, queryFulfilled }) {
        try {
          const { data } = await queryFulfilled
          const user = extractUserFromToken(data.token)
          if (user) {
            dispatch(setCredentials({ token: data.token, user }))
          }
        } catch {
          // errors handled by component
        }
      },
    }),
  }),
})

export const { useLoginMutation, useRegisterMutation } = authApi
