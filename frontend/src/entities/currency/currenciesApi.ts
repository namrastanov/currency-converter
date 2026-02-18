import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { CurrencyDto } from './types'

export const currenciesApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getCurrencies: builder.query<CurrencyDto[], void>({
      query: () => '/currencies',
      transformResponse: (response: ApiResponse<CurrencyDto[]>) => unwrapResponse(response),
      providesTags: ['Currencies'],
    }),
  }),
})

export const { useGetCurrenciesQuery } = currenciesApi
