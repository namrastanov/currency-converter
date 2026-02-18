export type LatestRatesDto = {
  baseCurrency: string
  date: string
  rates: Record<string, number>
}

export type ExchangeRate = {
  baseCurrency: string
  date: string
  rates: Record<string, number>
}

export type HistoricalRatesDto = {
  baseCurrency: string
  rates: ExchangeRate[]
  totalCount: number
  page: number
  pageSize: number
  totalPages: number
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export type ConversionResultDto = {
  from: string
  to: string
  amount: number
  result: number
  rate: number
  date: string
}
