import { useState } from 'react'
import { useGetUsersQuery, useCreateUserMutation, useUpdateUserRoleMutation, useDeleteUserMutation } from '@/features/admin/adminApi'
import { useAppSelector } from '@/app/hooks'
import { APP_ROLES } from '@/shared/lib/constants'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/shared/ui/card'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/shared/ui/table'
import { Select } from '@/shared/ui/select'
import { Button } from '@/shared/ui/button'
import { Badge } from '@/shared/ui/badge'
import { Skeleton } from '@/shared/ui/skeleton'
import { Tooltip } from '@/shared/ui/tooltip'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle, DialogTrigger } from '@/shared/ui/dialog'
import { Input } from '@/shared/ui/input'
import { Label } from '@/shared/ui/label'
import { Trash2, Loader2, UserPlus, ShieldAlert } from 'lucide-react'
import { format } from 'date-fns'
import { toast } from 'sonner'

export function UserManagementPage() {
  const currentUser = useAppSelector((state) => state.auth.user)
  const { data: users, isLoading } = useGetUsersQuery()
  const [createUser, { isLoading: isCreating }] = useCreateUserMutation()
  const [updateRole, { isLoading: isUpdating }] = useUpdateUserRoleMutation()
  const [deleteUser, { isLoading: isDeleting }] = useDeleteUserMutation()
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false)
  const [userToDelete, setUserToDelete] = useState<{ id: string; username: string } | null>(null)

  const [createDialogOpen, setCreateDialogOpen] = useState(false)
  const [newUsername, setNewUsername] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [newRole, setNewRole] = useState<(typeof APP_ROLES)[keyof typeof APP_ROLES]>(APP_ROLES.USER)

  function resetCreateForm() {
    setNewUsername('')
    setNewPassword('')
    setNewRole(APP_ROLES.USER)
  }

  async function handleCreateUser() {
    if (!newUsername.trim() || !newPassword.trim()) {
      toast.error('Username and password are required')
      return
    }
    try {
      await createUser({ username: newUsername.trim(), password: newPassword, role: newRole }).unwrap()
      toast.success(`User ${newUsername} created`)
      setCreateDialogOpen(false)
      resetCreateForm()
    } catch (err) {
      const apiErr = err as { data?: { detail?: string } }
      toast.error(apiErr?.data?.detail ?? 'Failed to create user')
    }
  }

  async function handleRoleChange(userId: string, newRole: string) {
    try {
      await updateRole({ id: userId, role: newRole }).unwrap()
      toast.success('User role updated')
    } catch {
      toast.error('Failed to update role')
    }
  }

  function confirmDelete(userId: string, username: string) {
    setUserToDelete({ id: userId, username })
    setDeleteDialogOpen(true)
  }

  async function handleDelete() {
    if (!userToDelete) return
    try {
      await deleteUser(userToDelete.id).unwrap()
      toast.success(`User ${userToDelete.username} deleted`)
    } catch (err) {
      const apiErr = err as { data?: { detail?: string } }
      toast.error(apiErr?.data?.detail ?? 'Failed to delete user')
    } finally {
      setDeleteDialogOpen(false)
      setUserToDelete(null)
    }
  }

  return (
    <div className="mx-auto max-w-3xl">
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <CardTitle>User Management</CardTitle>
              <CardDescription>View, create, change roles, and delete users</CardDescription>
            </div>
            <Button onClick={() => setCreateDialogOpen(true)}>
              <UserPlus className="mr-2 h-4 w-4" />
              Add User
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {Array.from({ length: 4 }).map((_, i) => (
                <Skeleton key={i} className="h-12 w-full" />
              ))}
            </div>
          ) : users && users.length > 0 ? (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>User</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Created At</TableHead>
                  <TableHead className="text-right">Actions</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {users.map((user) => {
                  const isSelf = user.id === currentUser?.id
                  const isDefaultAdmin = user.username.toLowerCase() === 'admin'
                  return (
                    <TableRow key={user.id}>
                      <TableCell className="font-medium">
                        {user.username}
                        {isSelf && <Badge variant="secondary" className="ml-2">You</Badge>}
                        {isDefaultAdmin && (
                          <Badge variant="default" className="ml-2">
                            <ShieldAlert className="mr-1 h-3 w-3" />
                            Default Admin
                          </Badge>
                        )}
                      </TableCell>
                      <TableCell>
                        {isDefaultAdmin ? (
                          <Tooltip content="Default admin role cannot be changed">
                            <span className="text-sm text-muted-foreground">{user.role}</span>
                          </Tooltip>
                        ) : (
                          <Select
                            value={user.role}
                            onChange={(e) => handleRoleChange(user.id, e.target.value)}
                            className="w-28"
                            disabled={isUpdating}
                          >
                            <option value={APP_ROLES.USER}>{APP_ROLES.USER}</option>
                            <option value={APP_ROLES.ADMIN}>{APP_ROLES.ADMIN}</option>
                          </Select>
                        )}
                      </TableCell>
                      <TableCell className="text-muted-foreground">
                        {format(new Date(user.createdAt), 'MM/dd/yyyy HH:mm')}
                      </TableCell>
                      <TableCell className="text-right">
                        {isDefaultAdmin || isSelf ? (
                          <Tooltip content={isSelf ? 'Cannot delete your own account' : 'Default admin cannot be deleted'}>
                            <Button variant="ghost" size="icon" disabled>
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </Tooltip>
                        ) : (
                          <Button
                            variant="ghost"
                            size="icon"
                            onClick={() => confirmDelete(user.id, user.username)}
                            disabled={isDeleting}
                          >
                            <Trash2 className="h-4 w-4 text-destructive" />
                          </Button>
                        )}
                      </TableCell>
                    </TableRow>
                  )
                })}
              </TableBody>
            </Table>
          ) : (
            <p className="py-8 text-center text-muted-foreground">No users found</p>
          )}
        </CardContent>
      </Card>

      {/* Create User Dialog */}
      <Dialog open={createDialogOpen} onOpenChange={(open) => { setCreateDialogOpen(open); if (!open) resetCreateForm() }}>
        <DialogTrigger asChild><span /></DialogTrigger>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Create New User</DialogTitle>
            <DialogDescription>
              Fill in the details to create a new user account.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-4 py-4">
            <div className="space-y-2">
              <Label htmlFor="new-username">Username</Label>
              <Input
                id="new-username"
                placeholder="Enter username"
                value={newUsername}
                onChange={(e) => setNewUsername(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="new-password">Password</Label>
              <Input
                id="new-password"
                type="password"
                placeholder="Enter password (min 6 characters)"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="new-role">Role</Label>
              <Select
                id="new-role"
                value={newRole}
                onChange={(e) => setNewRole(e.target.value as (typeof APP_ROLES)[keyof typeof APP_ROLES])}
              >
                <option value={APP_ROLES.USER}>{APP_ROLES.USER}</option>
                <option value={APP_ROLES.ADMIN}>{APP_ROLES.ADMIN}</option>
              </Select>
            </div>
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => { setCreateDialogOpen(false); resetCreateForm() }}>
              Cancel
            </Button>
            <Button onClick={handleCreateUser} disabled={isCreating}>
              {isCreating && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
              Create User
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      {/* Delete User Dialog */}
      <Dialog open={deleteDialogOpen} onOpenChange={setDeleteDialogOpen}>
        <DialogTrigger asChild><span /></DialogTrigger>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Delete User</DialogTitle>
            <DialogDescription>
              Are you sure you want to delete user <strong>{userToDelete?.username}</strong>? This action cannot be undone.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleteDialogOpen(false)}>
              Cancel
            </Button>
            <Button variant="destructive" onClick={handleDelete} disabled={isDeleting}>
              {isDeleting && <Loader2 className="h-4 w-4 animate-spin" />}
              Delete
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  )
}
