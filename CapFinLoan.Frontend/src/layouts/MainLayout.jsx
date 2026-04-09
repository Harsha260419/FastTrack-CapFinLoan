import { Outlet } from 'react-router-dom'

function MainLayout() {
  return (
    <main className="min-h-screen w-full bg-slate-50 text-slate-900">
      <Outlet />
    </main>
  )
}

export default MainLayout
