import { baseApi } from '@/shared/api/baseApi'
import type { ApiResponse } from '@/shared/api/types'
import { unwrapResponse } from '@/shared/api/types'
import type { ConversionResultDto } from '@/entities/rate/types'

type ConvertRequest = {
  from: string
  to: string
  amount: number
}

export const conversionApi = baseApi.injectEndpoints({
  endpoints: (builder) => ({
    convert: builder.query<ConversionResultDto, ConvertRequest>({
      query: ({ from, to, amount }) => ({
        url: '/convert',
        params: { from, to, amount },
      }),
      transformResponse: (response: ApiResponse<ConversionResultDto>) => unwrapResponse(response),
    }),
  }),
})

export const { useLazyConvertQuery } = conversionApi
