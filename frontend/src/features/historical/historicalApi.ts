import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { HistoricalRatesDto } from '@/entities/rate/types'

type HistoricalRatesRequest = {
  base: string
  from: string
  to: string
  page: number
  pageSize: number
  timezoneOffset: number
}

export const historicalApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    getHistoricalRates: builder.query<HistoricalRatesDto, HistoricalRatesRequest>({
      query: (params) => ({
        url: '/rates/historical',
        params,
      }),
      transformResponse: (response: ApiResponse<HistoricalRatesDto>) => unwrapResponse(response),
    }),
  }),
})

export const { useGetHistoricalRatesQuery, useLazyGetHistoricalRatesQuery } = historicalApi
