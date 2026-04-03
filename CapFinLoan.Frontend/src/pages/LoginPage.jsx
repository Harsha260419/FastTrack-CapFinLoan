import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'
import useAuthStore from '../store/authStore'

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
    <section className="flex min-h-screen w-full items-center justify-center bg-slate-100 p-4 sm:p-6">
      <div className="relative w-full max-w-md overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl">
        <div className="absolute inset-y-0 left-0 w-2 bg-[#0f1f3d]" />

        <div className="px-8 py-10">
          <div className="mb-8 flex items-center gap-3">
            <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-600 text-sm font-bold text-white">
              CF
            </span>
            <div>
              <p className="text-base font-semibold text-slate-900">CapFinLoan</p>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">Welcome Back</h1>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div>
              <label htmlFor="email" className="mb-1 block text-sm font-medium text-slate-700">
                Email
              </label>
              <input
                id="email"
                type="email"
                className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
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
                className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
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
              className="w-full rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-70"
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
    </section>
  )
}

export default LoginPage
