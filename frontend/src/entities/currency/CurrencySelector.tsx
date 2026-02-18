import { useGetCurrenciesQuery } from './currenciesApi'
import { Select } from '@/shared/ui/select'
import { Skeleton } from '@/shared/ui/skeleton'

type CurrencySelectorProps = {
  value: string
  onChange: (value: string) => void
  disabled?: boolean
  id?: string
  excludeRestricted?: boolean
}

export function CurrencySelector({ value, onChange, disabled, id, excludeRestricted = false }: CurrencySelectorProps) {
  const { data: currencies, isLoading } = useGetCurrenciesQuery()

  if (isLoading) return <Skeleton className="h-9 w-full" />

  return (
    <Select
      id={id}
      value={value}
      onChange={(e) => onChange(e.target.value)}
      disabled={disabled}
    >
      <option value="">Select currency</option>
      {currencies
        ?.filter((c) => !excludeRestricted || !c.isRestricted)
        .map((c) => (
          <option
            key={c.code}
            value={c.code}
            disabled={c.isRestricted}
          >
            {c.code} — {c.name}{c.isRestricted ? ' (restricted)' : ''}
          </option>
        ))}
    </Select>
  )
}
