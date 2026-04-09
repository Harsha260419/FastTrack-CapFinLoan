import { FileUp, LayoutDashboard, List } from 'lucide-react'
import { Outlet } from 'react-router-dom'
import Navbar from '../components/Navbar'
import Sidebar from '../components/Sidebar'

const applicantLinks = [
  { label: 'Dashboard', to: '/applicant/dashboard', icon: LayoutDashboard },
  { label: 'My Applications', to: '/applicant/applications', icon: List },
  { label: 'Apply Loan', to: '/applicant/apply', icon: FileUp },
]

function ApplicantLayout() {
  return (
    <div className="fixed inset-0 z-20 min-h-screen w-screen bg-slate-100 lg:grid lg:grid-cols-[18rem_1fr]">
      <Sidebar links={applicantLinks} />

      <div className="flex min-h-screen min-w-0 flex-col lg:col-start-2">
        <Navbar />
        <main className="flex-1 overflow-y-auto bg-white p-4 sm:p-6 lg:p-8">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

export default ApplicantLayout
