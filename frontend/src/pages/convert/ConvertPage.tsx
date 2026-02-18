import { ConversionForm } from '@/features/conversion/ConversionForm'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/ui/card'

export function ConvertPage() {
  return (
    <div className="mx-auto max-w-lg">
      <Card>
        <CardHeader>
          <CardTitle>Currency Conversion</CardTitle>
          <CardDescription>Convert amounts between different currencies</CardDescription>
        </CardHeader>
        <CardContent>
          <ConversionForm />
        </CardContent>
      </Card>
    </div>
  )
}
