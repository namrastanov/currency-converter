import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { LatestRatesDto } from './types'

export const ratesApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getLatestRates: builder.query<LatestRatesDto, string>({
      query: (baseCurrency) => ({
        url: '/rates/latest',
        params: { base: baseCurrency },
      }),
      transformResponse: (response: ApiResponse<LatestRatesDto>) => unwrapResponse(response),
      providesTags: ['Rates'],
    }),
  }),
})

export const { useGetLatestRatesQuery, useLazyGetLatestRatesQuery } = ratesApi
