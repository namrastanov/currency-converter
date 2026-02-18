import { createBrowserRouter } from 'react-router-dom'
import { Layout } from '@/widgets/layout/Layout'
import { ProtectedRoute } from '@/features/auth/ProtectedRoute'
import { AdminRoute } from '@/features/auth/AdminRoute'
import { LoginPage } from '@/pages/login/LoginPage'
import { RegisterPage } from '@/pages/register/RegisterPage'
import { ConvertPage } from '@/pages/convert/ConvertPage'
import { RatesPage } from '@/pages/rates/RatesPage'
import { HistoricalPage } from '@/pages/historical/HistoricalPage'
import { UserManagementPage } from '@/pages/admin/UserManagementPage'

export const router = createBrowserRouter([
  {
    path: '/login',
    element: <LoginPage />,
  },
  {
    path: '/register',
    element: <RegisterPage />,
  },
  {
    element: <ProtectedRoute />,
    children: [
      {
        element: <Layout />,
        children: [
          { index: true, element: <ConvertPage /> },
          { path: 'convert', element: <ConvertPage /> },
          { path: 'rates', element: <RatesPage /> },
          { path: 'historical', element: <HistoricalPage /> },
          {
            element: <AdminRoute />,
            children: [
              { path: 'admin/users', element: <UserManagementPage /> },
            ],
          },
        ],
      },
    ],
  },
])
