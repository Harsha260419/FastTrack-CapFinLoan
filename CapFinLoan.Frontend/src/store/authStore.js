import { create } from 'zustand'

const useAuthStore = create((set) => ({
  user: null,
  token: sessionStorage.getItem('token'),
  role: null,
  setAuth: (user, token, role) => {
    sessionStorage.setItem('token', token)
    set({ user, token, role })
  },
  logout: () => {
    sessionStorage.clear()
    set({ user: null, token: null, role: null })
  },
}))

export default useAuthStore
