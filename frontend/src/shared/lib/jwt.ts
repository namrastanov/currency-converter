import type { AuthUser } from '@/entities/user/types'

type JwtPayload = {
  sub: string
  [key: string]: unknown
}

function decodeBase64Url(str: string): string {
  const base64 = str.replace(/-/g, '+').replace(/_/g, '/')
  const padded = base64 + '='.repeat((4 - (base64.length % 4)) % 4)
  return atob(padded)
}

export function decodeJwt(token: string): JwtPayload | null {
  try {
    const parts = token.split('.')
    if (parts.length !== 3) return null
    const payload = JSON.parse(decodeBase64Url(parts[1]))
    return payload as JwtPayload
  } catch {
    return null
  }
}

export function isTokenExpired(token: string): boolean {
  const payload = decodeJwt(token)
  if (!payload?.exp) return true
  const exp = typeof payload.exp === 'number' ? payload.exp : Number(payload.exp)
  return Date.now() >= exp * 1000
}

export function extractUserFromToken(token: string): AuthUser | null {
  const payload = decodeJwt(token)
  if (!payload) return null

  const id = String(payload.sub ?? '')
  const username = String(
    payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ??
    payload.name ??
    ''
  )
  const role = String(
    payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ??
    payload.role ??
    ''
  )

  if (!id || !username) return null
  return { id, username, role }
}
