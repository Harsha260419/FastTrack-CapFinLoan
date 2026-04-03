import { LogOut } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import useAuthStore from '../store/authStore'

function Navbar() {
  const navigate = useNavigate()
  const user = useAuthStore((state) => state.user)
  const logout = useAuthStore((state) => state.logout)

  const displayName =
    user?.name || user?.fullName || user?.firstName || user?.username || 'User'

  const handleLogout = () => {
    logout()
    navigate('/login')
  }

  return (
    <header className="flex h-16 items-center justify-between border-b border-slate-200 bg-white px-4 sm:px-6">
      <div className="flex items-center gap-3">
        <span className="inline-flex h-9 w-9 items-center justify-center rounded-lg bg-blue-600 font-semibold text-white">
          CF
        </span>
        <div>
          <p className="text-lg font-semibold tracking-tight text-slate-900">CapFinLoan</p>
        </div>
      </div>

      <div className="flex items-center gap-3">
        <span className="hidden text-sm font-medium text-slate-700 sm:inline">
          {displayName}
        </span>
        <button
          type="button"
          onClick={handleLogout}
          className="inline-flex items-center gap-2 rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium text-slate-700 transition hover:border-red-300 hover:text-red-600"
        >
          <LogOut size={16} />
          Logout
        </button>
      </div>
    </header>
  )
}

export default Navbar
