import * as React from 'react'
import { cn } from '@/shared/lib/utils'

function Tooltip({ children, content }: { children: React.ReactNode; content: string }) {
  const [visible, setVisible] = React.useState(false)

  return (
    <div
      className="relative inline-flex"
      onMouseEnter={() => setVisible(true)}
      onMouseLeave={() => setVisible(false)}
    >
      {children}
      {visible && (
        <div
          className={cn(
            'absolute bottom-full left-1/2 z-50 mb-2 -translate-x-1/2 rounded-md bg-primary px-3 py-1.5 text-xs text-primary-foreground shadow-md whitespace-nowrap'
          )}
        >
          {content}
        </div>
      )}
    </div>
  )
}

export { Tooltip }
