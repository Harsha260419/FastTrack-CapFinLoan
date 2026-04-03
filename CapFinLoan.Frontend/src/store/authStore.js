import { create } from 'zustand'

const useAuthStore = create((set) => ({
  auth: null,
  user: null,
  email: null,
  userId: null,
  token: sessionStorage.getItem('token'),
  role: null,
  setAuth: (authResponse) => {
    const token = authResponse?.token || null
    const role = authResponse?.role || null
    const user = authResponse?.user || null
    const email = authResponse?.email || null
    const userId = authResponse?.userId || null

    sessionStorage.setItem('token', token)
    set({ auth: authResponse, user, email, userId, token, role })
  },
  logout: () => {
    sessionStorage.clear()
    set({ auth: null, user: null, email: null, userId: null, token: null, role: null })
  },
}))

export default useAuthStore
