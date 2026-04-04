import { create } from 'zustand'

const useAuthStore = create((set) => ({
  auth: null,
  user: null,
  email: sessionStorage.getItem('email'),
  userId: null,
  token: sessionStorage.getItem('token'),
  role: sessionStorage.getItem('role'),
  setAuth: (authResponse) => {
    const token = authResponse?.token || null
    const role = authResponse?.role || null
    const user = authResponse?.user || null
    const email = authResponse?.email || null
    const userId = authResponse?.userId || null

    if (token) {
      sessionStorage.setItem('token', token)
    } else {
      sessionStorage.removeItem('token')
    }

    if (email) {
      sessionStorage.setItem('email', email)
    } else {
      sessionStorage.removeItem('email')
    }

    if (role) {
      sessionStorage.setItem('role', role)
    } else {
      sessionStorage.removeItem('role')
    }

    set({ auth: authResponse, user, email, userId, token, role })
  },
  logout: () => {
    sessionStorage.clear()
    set({ auth: null, user: null, email: null, userId: null, token: null, role: null })
  },
}))

export default useAuthStore
