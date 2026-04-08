import { BarChart3, LayoutDashboard, ListChecks } from 'lucide-react'
import { Outlet } from 'react-router-dom'
import Navbar from '../components/Navbar'
import Sidebar from '../components/Sidebar'

const adminLinks = [
  { label: 'Dashboard', to: '/admin/dashboard', icon: LayoutDashboard },
  { label: 'Applications Queue', to: '/admin/queue', icon: ListChecks },
  { label: 'Reports', to: '/admin/reports', icon: BarChart3 },
]

function AdminLayout() {
  return (
    <div className="fixed inset-0 z-20 min-h-screen w-screen bg-slate-100 lg:grid lg:grid-cols-[18rem_1fr]">
      <Sidebar links={adminLinks} />

      <div className="flex min-h-screen min-w-0 flex-col lg:col-start-2">
        <Navbar />
        <main className="flex-1 overflow-y-auto bg-white p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default AdminLayout
