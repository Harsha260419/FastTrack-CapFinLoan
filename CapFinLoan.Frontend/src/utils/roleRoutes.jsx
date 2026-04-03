import { Navigate, Outlet } from 'react-router-dom'
import useAuthStore from '../store/authStore'

export function RequireAuth() {
  const token = useAuthStore((state) => state.token)

  if (!token && !sessionStorage.getItem('token')) {
    return <Navigate to="/login" replace />
  }

  return <Outlet />
}

export function RequireAdmin() {
  const role = useAuthStore((state) => state.role)

  if (role !== 'ADMIN') {
    return <Navigate to="/" replace />
  }

  return <Outlet />
}
