import {
  CheckCircle,
  FileText,
  Shield,
  TrendingUp,
  Zap,
} from 'lucide-react'
import { useNavigate } from 'react-router-dom'

function LandingPage() {
  const navigate = useNavigate()

  const scrollToFeatures = () => {
    const target = document.getElementById('features')
    if (target) {
      target.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }

  return (
    <div className="min-h-screen w-full bg-gray-50 text-slate-900">
      <header className="sticky top-0 z-40 border-b border-slate-200 bg-white/95 backdrop-blur">
        <nav className="mx-auto flex w-full max-w-6xl items-center justify-between px-8 py-3">
          <div className="flex items-center gap-3">
            <span className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-slate-900 text-sm font-bold text-white">
              CF
            </span>
            <span className="text-lg font-semibold tracking-tight text-slate-900">CapFinLoan</span>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => navigate('/login')}
              className="rounded-lg border border-slate-900 px-4 py-2 text-sm font-semibold text-slate-900 transition hover:bg-slate-100"
            >
              Login
            </button>
            <button
              type="button"
              onClick={() => navigate('/signup')}
              className="rounded-lg bg-slate-900 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-700"
            >
              Get Started
            </button>
          </div>
        </nav>
      </header>

      <main>
        <section className="w-full bg-gray-50 pb-14 pt-16 text-center lg:pt-20">
          <div className="mx-auto w-full max-w-6xl px-8">
            <h1 className="text-4xl font-extrabold tracking-tight text-slate-900 sm:text-5xl lg:text-6xl">
              Loan Approvals, Reimagined
            </h1>
            <p className="mx-auto mt-5 max-w-3xl text-base text-slate-600 sm:text-lg">
              Apply online, upload documents securely, and track your approval in real time - all in one place.
            </p>

            <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
              <button
                type="button"
                onClick={() => navigate('/signup')}
                className="rounded-lg bg-slate-900 px-5 py-3 text-sm font-semibold text-white transition hover:bg-slate-700"
              >
                Apply Now
              </button>
              <button
                type="button"
                onClick={scrollToFeatures}
                className="rounded-lg border border-slate-900 bg-white px-5 py-3 text-sm font-semibold text-slate-900 transition hover:bg-slate-100"
              >
                Learn More
              </button>
            </div>

            <div className="mt-8 flex flex-wrap items-center justify-center gap-3">
              <span className="rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700">500+ Applications Processed</span>
              <span className="rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700">99% Secure</span>
              <span className="rounded-full border border-slate-200 bg-white px-4 py-2 text-sm font-medium text-slate-700">24hr Avg. Approval</span>
            </div>
          </div>
        </section>

        <section id="features" className="w-full bg-gray-50 py-14">
          <div className="mx-auto w-full max-w-6xl px-8">
            <div className="mb-10 text-center">
              <h2 className="text-3xl font-bold tracking-tight text-slate-900">Everything you need, in one platform</h2>
            </div>

            <div className="grid gap-5 md:grid-cols-3">
              <article className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="mb-4 inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
                  <Zap size={20} />
                </div>
                <h3 className="text-lg font-semibold text-slate-900">Quick Application</h3>
                <p className="mt-2 text-sm leading-6 text-slate-600">Complete your loan application in a guided 4-step process.</p>
              </article>

              <article className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="mb-4 inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
                  <FileText size={20} />
                </div>
                <h3 className="text-lg font-semibold text-slate-900">Document Management</h3>
                <p className="mt-2 text-sm leading-6 text-slate-600">Upload and manage your documents securely with real-time verification.</p>
              </article>

              <article className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
                <div className="mb-4 inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-50 text-blue-600">
                  <TrendingUp size={20} />
                </div>
                <h3 className="text-lg font-semibold text-slate-900">Live Status Tracking</h3>
                <p className="mt-2 text-sm leading-6 text-slate-600">Track every stage of your application with a detailed status timeline.</p>
              </article>
            </div>
          </div>
        </section>

        <section className="w-full bg-gray-50 pb-16 pt-4">
          <div className="mx-auto w-full max-w-6xl px-8">
            <div className="rounded-2xl border border-slate-200 bg-white p-6 sm:p-8">
              <h2 className="text-center text-3xl font-bold tracking-tight text-slate-900">How it works</h2>

              <div className="relative mt-10 grid gap-8 md:grid-cols-4">
                <div className="pointer-events-none absolute left-0 right-0 top-5 hidden h-px bg-slate-200 md:block" />

                {[
                  { step: '1', title: 'Create Account', icon: Shield },
                  { step: '2', title: 'Submit Application', icon: Zap },
                  { step: '3', title: 'Upload Documents', icon: FileText },
                  { step: '4', title: 'Get Approved', icon: CheckCircle },
                ].map((item) => {
                  const Icon = item.icon
                  return (
                    <div key={item.step} className="relative text-center">
                      <div className="mx-auto mb-3 inline-flex h-10 w-10 items-center justify-center rounded-full bg-blue-600 text-sm font-bold text-white">
                        {item.step}
                      </div>
                      <div className="mx-auto mb-2 inline-flex h-10 w-10 items-center justify-center rounded-xl bg-slate-100 text-slate-700">
                        <Icon size={18} />
                      </div>
                      <p className="text-sm font-semibold text-slate-800">{item.title}</p>
                    </div>
                  )
                })}
              </div>
            </div>
          </div>
        </section>
      </main>

      <footer className="bg-slate-900 py-4 text-center text-sm text-white">
        © 2026 CapFinLoan. All rights reserved.
      </footer>
    </div>
  )
}

export default LandingPage
