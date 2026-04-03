import { Outlet } from 'react-router-dom'

function MainLayout() {
  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      <div className="mx-auto max-w-5xl px-4 py-10">
        <Outlet />
      </div>
    </main>
  )
}

export default MainLayout
