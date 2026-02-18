import { useState } from 'react'
import { useGetHistoricalRatesQuery } from '@/features/historical/historicalApi'
import { Pagination } from '@/features/historical/Pagination'
import { CurrencySelector } from '@/entities/currency/CurrencySelector'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table'
import { Select } from '@/shared/ui/select'
import { Skeleton } from '@/shared/ui/skeleton'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { Button } from '@/shared/ui/button'
import { Search } from 'lucide-react'
import { format, subDays, differenceInDays } from 'date-fns'
import { MAX_HISTORICAL_RANGE_DAYS, PAGE_SIZES } from '@/shared/lib/constants'

export function HistoricalPage() {
  const today = format(new Date(), 'yyyy-MM-dd')
  const defaultFrom = format(subDays(new Date(), 30), 'yyyy-MM-dd')

  const [baseCurrency, setBaseCurrency] = useState('EUR')
  const [fromDate, setFromDate] = useState(defaultFrom)
  const [toDate, setToDate] = useState(today)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(10)
  const [searchParams, setSearchParams] = useState<{ base: string; from: string; to: string } | null>(null)
  const [validationError, setValidationError] = useState('')

  const { data, isLoading, isFetching } = useGetHistoricalRatesQuery(
    {
      base: searchParams?.base ?? '',
      from: searchParams?.from ?? '',
      to: searchParams?.to ?? '',
      page,
      pageSize,
      timezoneOffset: new Date().getTimezoneOffset(),
    },
    { skip: !searchParams }
  )

  function handleSearch(e: React.FormEvent) {
    e.preventDefault()
    setValidationError('')

    if (!baseCurrency) { setValidationError('Please select a base currency'); return }
    if (!fromDate || !toDate) { setValidationError('Please specify both dates'); return }
    if (fromDate > toDate) { setValidationError('Start date cannot be later than end date'); return }
    if (toDate > today) { setValidationError('End date cannot be in the future'); return }
    const days = differenceInDays(new Date(toDate), new Date(fromDate))
    if (days > MAX_HISTORICAL_RANGE_DAYS) {
      setValidationError(`Maximum range is ${MAX_HISTORICAL_RANGE_DAYS} days (2 years)`)
      return
    }

    setPage(1)
    setSearchParams({ base: baseCurrency, from: fromDate, to: toDate })
  }

  function handlePageSizeChange(newSize: number) {
    setPageSize(newSize)
    setPage(1)
  }

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      <Card>
        <CardHeader>
          <CardTitle>Historical Rates</CardTitle>
          <CardDescription>View exchange rates for a selected period</CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={handleSearch} className="space-y-4">
            <div className="grid gap-4 sm:grid-cols-3">
              <div className="space-y-2">
                <Label>Base Currency</Label>
                <CurrencySelector value={baseCurrency} onChange={setBaseCurrency} disabled={isFetching} />
              </div>
              <div className="space-y-2">
                <Label htmlFor="from-date">Start Date</Label>
                <Input
                  id="from-date"
                  type="date"
                  value={fromDate}
                  onChange={(e) => setFromDate(e.target.value)}
                  max={today}
                  disabled={isFetching}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="to-date">End Date</Label>
                <Input
                  id="to-date"
                  type="date"
                  value={toDate}
                  onChange={(e) => setToDate(e.target.value)}
                  max={today}
                  disabled={isFetching}
                />
              </div>
            </div>
            {validationError && <p className="text-sm text-destructive">{validationError}</p>}
            <Button type="submit" disabled={isFetching}>
              <Search className="h-4 w-4" />
              Search
            </Button>
          </form>
        </CardContent>
      </Card>

      {searchParams && (
        <Card>
          <CardContent className="pt-6 space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                Base Currency: <span className="font-medium text-foreground">{searchParams.base}</span>
              </p>
              <div className="flex items-center gap-2">
                <Label htmlFor="page-size" className="text-sm whitespace-nowrap">Per page:</Label>
                <Select
                  id="page-size"
                  value={pageSize}
                  onChange={(e) => handlePageSizeChange(Number(e.target.value))}
                  className="w-20"
                  disabled={isFetching}
                >
                  {PAGE_SIZES.map((s) => (
                    <option key={s} value={s}>{s}</option>
                  ))}
                </Select>
              </div>
            </div>

            {isLoading ? (
              <div className="space-y-2">
                {Array.from({ length: 5 }).map((_, i) => (
                  <Skeleton key={i} className="h-10 w-full" />
                ))}
              </div>
            ) : data && data.rates.length > 0 ? (
              <>
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Date</TableHead>
                      <TableHead className="text-right">Currency Count</TableHead>
                      <TableHead className="text-right">Sample Rate</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {data.rates.map((rate) => {
                      const entries = Object.entries(rate.rates)
                      const usdRate = rate.rates['USD']
                      return (
                        <TableRow key={rate.date}>
                          <TableCell className="font-medium">
                            {format(new Date(rate.date), 'MM/dd/yyyy')}
                          </TableCell>
                          <TableCell className="text-right">{entries.length}</TableCell>
                          <TableCell className="text-right font-mono">
                            {usdRate
                              ? `USD: ${usdRate.toLocaleString('en-US', { minimumFractionDigits: 4 })}`
                              : entries.length > 0
                                ? `${entries[0][0]}: ${entries[0][1].toLocaleString('en-US', { minimumFractionDigits: 4 })}`
                                : '—'
                            }
                          </TableCell>
                        </TableRow>
                      )
                    })}
                  </TableBody>
                </Table>
                <Pagination
                  page={data.page}
                  totalPages={data.totalPages}
                  totalCount={data.totalCount}
                  hasNextPage={data.hasNextPage}
                  hasPreviousPage={data.hasPreviousPage}
                  onPageChange={setPage}
                />
              </>
            ) : (
              <p className="py-8 text-center text-muted-foreground">
                No data for the selected period
              </p>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  )
}
