import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { UserDto } from '@/entities/user/types'

export type CreateUserPayload = {
  username: string
  password: string
  role: string
}

export const adminApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getUsers: builder.query<UserDto[], void>({
      query: () => '/admin/users',
      transformResponse: (response: ApiResponse<UserDto[]>) => unwrapResponse(response),
      providesTags: ['Users'],
    }),

    createUser: builder.mutation<UserDto, CreateUserPayload>({
      query: (body) => ({
        url: '/admin/users',
        method: 'POST',
        body,
      }),
      transformResponse: (response: ApiResponse<UserDto>) => unwrapResponse(response),
      invalidatesTags: ['Users'],
    }),

    updateUserRole: builder.mutation<UserDto, { id: string; role: string }>({
      query: ({ id, role }) => ({
        url: `/admin/users/${id}/role`,
        method: 'PUT',
        body: { role },
      }),
      transformResponse: (response: ApiResponse<UserDto>) => unwrapResponse(response),
      invalidatesTags: ['Users'],
    }),

    deleteUser: builder.mutation<void, string>({
      query: (id) => ({
        url: `/admin/users/${id}`,
        method: 'DELETE',
      }),
      invalidatesTags: ['Users'],
    }),
  }),
})

export const {
  useGetUsersQuery,
  useCreateUserMutation,
  useUpdateUserRoleMutation,
  useDeleteUserMutation,
} = adminApi
