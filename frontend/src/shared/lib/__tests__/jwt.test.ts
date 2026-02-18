import { describe, it, expect } from 'vitest'
import { decodeJwt, isTokenExpired, extractUserFromToken } from '../jwt'

const VALID_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwYWIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImNsaWVudF9pZCI6IjEyMzQ1Njc4LTEyMzQtMTIzNC0xMjM0LTEyMzQ1Njc4OTBhYiIsImp0aSI6InRlc3Qtand0LWlkIiwiZXhwIjo5OTk5OTk5OTk5fQ.fake-signature'

const EXPIRED_TOKEN = 'eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3OC0xMjM0LTEyMzQtMTIzNC0xMjM0NTY3ODkwYWIiLCJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYWRtaW4iLCJodHRwOi8vc2NoZW1hcy5taWNyb3NvZnQuY29tL3dzLzIwMDgvMDYvaWRlbnRpdHkvY2xhaW1zL3JvbGUiOiJBZG1pbiIsImV4cCI6MX0.fake-signature'

describe('JWT utilities', () => {
  describe('decodeJwt', () => {
    it('decodes a valid token', () => {
      const payload = decodeJwt(VALID_TOKEN)
      expect(payload).not.toBeNull()
      expect(payload!.sub).toBe('12345678-1234-1234-1234-1234567890ab')
    })

    it('returns null for an invalid token', () => {
      expect(decodeJwt('not-a-jwt')).toBeNull()
      expect(decodeJwt('')).toBeNull()
      expect(decodeJwt('a.b')).toBeNull()
    })
  })

  describe('isTokenExpired', () => {
    it('returns false for a token with exp in the future', () => {
      expect(isTokenExpired(VALID_TOKEN)).toBe(false)
    })

    it('returns true for an expired token', () => {
      expect(isTokenExpired(EXPIRED_TOKEN)).toBe(true)
    })

    it('returns true for an invalid token', () => {
      expect(isTokenExpired('garbage')).toBe(true)
    })
  })

  describe('extractUserFromToken', () => {
    it('extracts user from token with .NET claims', () => {
      const user = extractUserFromToken(VALID_TOKEN)
      expect(user).toEqual({
        id: '12345678-1234-1234-1234-1234567890ab',
        username: 'admin',
        role: 'Admin',
      })
    })

    it('returns null for an invalid token', () => {
      expect(extractUserFromToken('bad')).toBeNull()
    })
  })
})
