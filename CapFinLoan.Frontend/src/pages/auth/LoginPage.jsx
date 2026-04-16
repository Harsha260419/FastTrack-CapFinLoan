import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { CheckCircle } from 'lucide-react'
import { Link, useNavigate } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import useAuthStore from '../../store/authStore'

function LoginPage() {
  const navigate = useNavigate()
  const setAuth = useAuthStore((state) => state.setAuth)
  const [loginError, setLoginError] = useState('')
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm({
    defaultValues: {
      email: '',
      password: '',
    },
  })

  const onSubmit = async (values) => {
    setLoginError('')

    try {
      const response = await axiosInstance.post('/gateway/auth/login', {
        email: values.email,
        password: values.password,
      })

      const authResponse = response.data || {}
      const { role } = authResponse
      const normalizedRole = String(role || '').toUpperCase()

      setAuth({ ...authResponse, role: normalizedRole })

      if (normalizedRole === 'ADMIN') {
        navigate('/admin/dashboard')
        return
      }

      navigate('/applicant/dashboard')
    } catch (error) {
      setLoginError('Invalid credentials')
    }
  }

  return (
    <section className="min-h-screen w-full bg-gray-50">
      <div className="flex min-h-screen w-full">
        <aside className="relative hidden min-h-screen w-3/5 overflow-hidden bg-slate-900 p-10 md:flex md:flex-col">
          <div className="relative z-10 flex h-full flex-col">
            <div className="flex items-center gap-3">
              <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-white text-sm font-bold text-slate-900">
                CF
              </span>
              <span className="text-xl font-semibold tracking-tight text-white">CapFinLoan</span>
            </div>

            <div className="mt-16 max-w-xl">
              <h1 className="text-5xl font-bold leading-tight text-white">Fast-track your loan approval</h1>

              <ul className="mt-10 space-y-4">
                {[
                  'Apply in minutes, get approved faster',
                  'Secure document upload & verification',
                  'Real-time application status tracking',
                ].map((item) => (
                  <li key={item} className="flex items-start gap-3 text-base text-slate-100">
                    <CheckCircle size={20} className="mt-0.5 text-white" />
                    <span>{item}</span>
                  </li>
                ))}
              </ul>
            </div>

            <div className="pointer-events-none absolute -bottom-20 -left-10 h-56 w-56 rounded-full bg-slate-700/30" />
            <div className="pointer-events-none absolute bottom-16 right-10 h-28 w-28 rounded-2xl bg-slate-800/40" />
            <div className="pointer-events-none absolute -right-10 top-32 h-44 w-44 rounded-full bg-slate-700/20" />
          </div>
        </aside>

        <div className="flex min-h-screen w-full items-center justify-center bg-gray-50 px-6 py-10 md:w-2/5 md:px-12 lg:px-16">
          <div className="w-full max-w-md">
            <div className="mb-8">
              <div className="mb-5 flex items-center gap-3">
                <span className="inline-flex h-10 w-10 items-center justify-center rounded-xl bg-slate-900 text-sm font-bold text-white">
                  CF
                </span>
                <p className="text-sm font-semibold text-slate-900">CapFinLoan</p>
              </div>
              <h2 className="text-3xl font-bold tracking-tight text-slate-900">Welcome Back</h2>
              <p className="mt-2 text-sm text-slate-600">Sign in to continue your application journey.</p>
            </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div>
              <label htmlFor="email" className="mb-1 block text-sm font-medium text-slate-700">
                Email
              </label>
              <input
                id="email"
                type="email"
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
                placeholder="you@example.com"
                {...register('email', {
                  required: 'Email is required',
                  pattern: {
                    value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                    message: 'Enter a valid email address',
                  },
                })}
              />
              {errors.email ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.email.message}</p>
              ) : null}
            </div>

            <div>
              <label htmlFor="password" className="mb-1 block text-sm font-medium text-slate-700">
                Password
              </label>
              <input
                id="password"
                type="password"
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
                placeholder="Enter your password"
                {...register('password', {
                  required: 'Password is required',
                })}
              />
              {errors.password ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.password.message}</p>
              ) : null}
            </div>

            {loginError ? (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                {loginError}
              </p>
            ) : null}

            <button
              type="submit"
              disabled={isSubmitting}
              className="w-full rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSubmitting ? 'Signing in...' : 'Login'}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-600">
            New here?{' '}
            <Link to="/signup" className="font-semibold text-blue-600 hover:text-blue-700">
              Create an account
            </Link>
          </p>
        </div>
        </div>
      </div>
    </section>
  )
}

export default LoginPage
