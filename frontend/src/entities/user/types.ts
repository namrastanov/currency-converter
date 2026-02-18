export type UserDto = {
  id: string
  username: string
  role: string
  createdAt: string
}

export type AuthResult = {
  token: string
  username: string
  role: string
}

export type AuthUser = {
  id: string
  username: string
  role: string
}
