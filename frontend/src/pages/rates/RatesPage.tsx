import { useState } from 'react'
import { useGetLatestRatesQuery } from '@/entities/rate/ratesApi'
import { useGetCurrenciesQuery } from '@/entities/currency/currenciesApi'
import { CurrencySelector } from '@/entities/currency/CurrencySelector'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table'
import { Badge } from '@/shared/ui/badge'
import { Skeleton } from '@/shared/ui/skeleton'
import { Button } from '@/shared/ui/button'
import { Label } from '@/shared/ui/label'
import { RefreshCw } from 'lucide-react'
import { format } from 'date-fns'

export function RatesPage() {
  const [baseCurrency, setBaseCurrency] = useState('EUR')
  const { data: currencies } = useGetCurrenciesQuery()
  const { data, isLoading, isFetching, refetch } = useGetLatestRatesQuery(baseCurrency, {
    skip: !baseCurrency,
  })

  const restrictedCodes = new Set(
    currencies?.filter((c) => c.isRestricted).map((c) => c.code) ?? []
  )

  return (
    <div className="mx-auto max-w-2xl">
      <Card>
        <CardHeader>
          <CardTitle>Latest Exchange Rates</CardTitle>
          <CardDescription>
            {data && `Last updated: ${format(new Date(data.date), 'MM/dd/yyyy')}`}
          </CardDescription>
        </CardHeader>
        <CardContent className="space-y-4">
          <div className="flex items-end gap-3">
            <div className="flex-1 space-y-2">
              <Label>Base Currency</Label>
              <CurrencySelector value={baseCurrency} onChange={setBaseCurrency} disabled={isFetching} />
            </div>
            <Button variant="outline" size="icon" onClick={() => refetch()} disabled={isFetching}>
              <RefreshCw className={`h-4 w-4 ${isFetching ? 'animate-spin' : ''}`} />
            </Button>
          </div>

          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 8 }).map((_, i) => (
                <Skeleton key={i} className="h-10 w-full" />
              ))}
            </div>
          ) : data ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Currency</TableHead>
                  <TableHead className="text-right">Rate</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {Object.entries(data.rates)
                  .sort(([a], [b]) => a.localeCompare(b))
                  .map(([code, rate]) => (
                    <TableRow
                      key={code}
                      className={restrictedCodes.has(code) ? 'opacity-50' : ''}
                    >
                      <TableCell className="flex items-center gap-2">
                        <span className="font-medium">{code}</span>
                        {restrictedCodes.has(code) && (
                          <Badge variant="secondary" className="text-xs">restricted</Badge>
                        )}
                      </TableCell>
                      <TableCell className="text-right font-mono">
                        {rate.toLocaleString('en-US', { minimumFractionDigits: 4, maximumFractionDigits: 6 })}
                      </TableCell>
                    </TableRow>
                  ))}
              </TableBody>
            </Table>
          ) : (
            <p className="text-center text-muted-foreground">Please select a base currency</p>
          )}
        </CardContent>
      </Card>
    </div>
  )
}
