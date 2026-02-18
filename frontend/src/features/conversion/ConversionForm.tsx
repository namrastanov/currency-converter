import { useState } from 'react'
import { useForm, Controller } from 'react-hook-form'
import { z } from 'zod'
import { zodResolver } from '@hookform/resolvers/zod'
import { useLazyConvertQuery } from './conversionApi'
import { CurrencySelector } from '@/entities/currency/CurrencySelector'
import { Button } from '@/shared/ui/button'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { Card, CardContent } from '@/shared/ui/card'
import { ArrowLeftRight, Loader2 } from 'lucide-react'
import { format } from 'date-fns'
import { parseApiError } from '@/shared/lib/utils'

const conversionSchema = z.object({
  from: z.string().min(1, 'Please select a source currency'),
  to: z.string().min(1, 'Please select a target currency'),
  amount: z.string().min(1, 'Please enter an amount'),
}).refine((data) => data.from !== data.to || !data.from, {
  message: 'Source and target currencies must be different',
  path: ['to'],
}).refine((data) => {
  const num = parseFloat(data.amount)
  return !isNaN(num) && num > 0
}, {
  message: 'Please enter a valid amount greater than 0',
  path: ['amount'],
})

type ConversionFormValues = z.infer<typeof conversionSchema>

export function ConversionForm() {
  const [serverError, setServerError] = useState('')
  const [trigger, { data, isFetching }] = useLazyConvertQuery()

  const { register, handleSubmit, control, formState: { errors } } = useForm<ConversionFormValues>({
    resolver: zodResolver(conversionSchema),
    defaultValues: { from: '', to: '', amount: '' },
  })

  async function onSubmit(values: ConversionFormValues) {
    setServerError('')
    try {
      await trigger({ from: values.from, to: values.to, amount: parseFloat(values.amount) }).unwrap()
    } catch (err) {
      setServerError(parseApiError(err, 'Conversion error'))
    }
  }

  return (
    <div className="space-y-6">
      <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
        <div className="grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="from-currency">From</Label>
            <Controller
              name="from"
              control={control}
              render={({ field }) => (
                <CurrencySelector id="from-currency" value={field.value} onChange={field.onChange} disabled={isFetching} />
              )}
            />
            {errors.from && <p className="text-sm text-destructive">{errors.from.message}</p>}
          </div>
          <div className="space-y-2">
            <Label htmlFor="to-currency">To</Label>
            <Controller
              name="to"
              control={control}
              render={({ field }) => (
                <CurrencySelector id="to-currency" value={field.value} onChange={field.onChange} disabled={isFetching} />
              )}
            />
            {errors.to && <p className="text-sm text-destructive">{errors.to.message}</p>}
          </div>
        </div>
        <div className="space-y-2">
          <Label htmlFor="amount">Amount</Label>
          <Input
            id="amount"
            type="number"
            step="any"
            min="0.01"
            {...register('amount')}
            placeholder="100.00"
            disabled={isFetching}
          />
          {errors.amount && <p className="text-sm text-destructive">{errors.amount.message}</p>}
        </div>
        {serverError && <p className="text-sm text-destructive">{serverError}</p>}
        <Button type="submit" className="w-full" disabled={isFetching}>
          {isFetching ? <Loader2 className="h-4 w-4 animate-spin" /> : <ArrowLeftRight className="h-4 w-4" />}
          Convert
        </Button>
      </form>

      {data && (
        <Card>
          <CardContent className="pt-6">
            <div className="text-center space-y-2">
              <p className="text-3xl font-bold">
                {data.result.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} {data.to}
              </p>
              <p className="text-sm text-muted-foreground">
                {data.amount.toLocaleString('en-US')} {data.from} = {data.result.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 4 })} {data.to}
              </p>
              <p className="text-xs text-muted-foreground">
                Rate: 1 {data.from} = {data.rate.toLocaleString('en-US', { minimumFractionDigits: 4 })} {data.to} | Date: {format(new Date(data.date), 'MM/dd/yyyy')}
              </p>
            </div>
          </CardContent>
        </Card>
      )}
    </div>
  )
}
