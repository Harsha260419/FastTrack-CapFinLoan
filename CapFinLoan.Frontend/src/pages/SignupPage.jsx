import { Mail } from 'lucide-react'
import { useEffect, useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'

function SignupPage() {
  const navigate = useNavigate()
  const [otpMessage, setOtpMessage] = useState('')
  const [sendOtpError, setSendOtpError] = useState('')
  const [submitError, setSubmitError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [isSendingOtp, setIsSendingOtp] = useState(false)
  const [hasSentOtp, setHasSentOtp] = useState(false)
  const [countdown, setCountdown] = useState(0)

  const {
    register,
    handleSubmit,
    trigger,
    setError,
    watch,
    formState: { errors, isSubmitting },
  } = useForm({
    defaultValues: {
      fullName: '',
      email: '',
      phoneNumber: '',
      password: '',
      confirmPassword: '',
      otpCode: '',
    },
  })

  const passwordValue = watch('password')
  const otpCodeValue = watch('otpCode')

  useEffect(() => {
    if (countdown <= 0) {
      return undefined
    }

    const timer = window.setInterval(() => {
      setCountdown((prev) => (prev > 0 ? prev - 1 : 0))
    }, 1000)

    return () => window.clearInterval(timer)
  }, [countdown])

  const handleSendOtp = async () => {
    setOtpMessage('')
    setSendOtpError('')
    setSubmitError('')
    setSuccessMessage('')

    const isEmailValid = await trigger('email')
    if (!isEmailValid) {
      return
    }

    setIsSendingOtp(true)

    try {
      const email = watch('email')
      await axiosInstance.post('/gateway/auth/signup/send-otp', { email })
      setHasSentOtp(true)
      setOtpMessage('OTP sent to your email')
      setCountdown(30)
    } catch (error) {
      setSendOtpError('Unable to send OTP. Please try again.')
    } finally {
      setIsSendingOtp(false)
    }
  }

  const onSubmit = async (values) => {
    setSubmitError('')
    setSuccessMessage('')
    setSendOtpError('')

    if (!hasSentOtp || !values.otpCode) {
      setSubmitError('Please send OTP and enter the code to continue.')
      return
    }

    try {
      await axiosInstance.post('/gateway/auth/signup', {
        name: values.fullName,
        email: values.email,
        phoneNumber: values.phoneNumber,
        password: values.password,
        role: 'APPLICANT',
        otpCode: values.otpCode,
      })

      setSuccessMessage('Signup successful. Redirecting to login...')
      setTimeout(() => {
        navigate('/login')
      }, 1200)
    } catch (error) {
      const statusCode = error?.response?.status
      const message =
        error?.response?.data?.message || error?.response?.data?.error || ''
      const normalizedMessage = String(message).toLowerCase()

      if (statusCode === 409 || normalizedMessage.includes('duplicate') || normalizedMessage.includes('already exists')) {
        setError('email', {
          type: 'server',
          message: 'Email already exists',
        })
        return
      }

      if (normalizedMessage.includes('invalid otp') || normalizedMessage.includes('expired otp') || normalizedMessage.includes('otp')) {
        setError('otpCode', {
          type: 'server',
          message: 'Invalid or expired OTP',
        })
        return
      }

      setSubmitError('Unable to create account. Please try again.')
    }
  }

  return (
    <section className="flex min-h-screen w-full items-center justify-center bg-gray-100 p-4 sm:p-6">
      <div className="relative w-full max-w-md overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-xl">
        <div className="absolute inset-y-0 left-0 w-2 bg-[#0f1f3d]" />

        <div className="px-8 py-10">
          <div className="mb-8 flex items-center gap-3">
            <span className="inline-flex h-11 w-11 items-center justify-center rounded-xl bg-blue-600 text-sm font-bold text-white">
              CF
            </span>
            <div>
              <p className="text-base font-semibold text-slate-900">CapFinLoan</p>
              <h1 className="text-2xl font-bold tracking-tight text-slate-900">Create Account</h1>
            </div>
          </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div>
              <label htmlFor="fullName" className="mb-1 block text-sm font-medium text-slate-700">
                Full Name
              </label>
              <input
                id="fullName"
                type="text"
                className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
                placeholder="John Doe"
                {...register('fullName', {
                  required: 'Full name is required',
                })}
              />
              {errors.fullName ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.fullName.message}</p>
              ) : null}
            </div>

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
              <label htmlFor="phoneNumber" className="mb-1 block text-sm font-medium text-slate-700">
                Phone Number
              </label>
              <input
                id="phoneNumber"
                type="text"
                className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
                placeholder="9876543210"
                {...register('phoneNumber', {
                  required: 'Phone number is required',
                  pattern: {
                    value: /^\d{10}$/,
                    message: 'Phone number must be exactly 10 digits',
                  },
                })}
              />
              {errors.phoneNumber ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.phoneNumber.message}</p>
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
                placeholder="Minimum 8 characters"
                {...register('password', {
                  required: 'Password is required',
                  minLength: {
                    value: 8,
                    message: 'Password must be at least 8 characters',
                  },
                })}
              />
              {errors.password ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.password.message}</p>
              ) : null}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="mb-1 block text-sm font-medium text-slate-700">
                Confirm Password
              </label>
              <input
                id="confirmPassword"
                type="password"
                className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
                placeholder="Re-enter password"
                {...register('confirmPassword', {
                  required: 'Confirm password is required',
                  validate: (value) =>
                    value === passwordValue || 'Passwords do not match',
                })}
              />
              {errors.confirmPassword ? (
                <p className="mt-1 text-xs font-medium text-red-600">{errors.confirmPassword.message}</p>
              ) : null}
            </div>

            {hasSentOtp ? (
              <div className="rounded-xl border border-blue-100 bg-blue-50/60 p-4">
                <p className="mb-3 inline-flex items-center gap-2 text-xs font-medium text-blue-700">
                  <Mail size={14} />
                  Check your email for the OTP
                </p>

                <label htmlFor="otpCode" className="mb-1 block text-center text-sm font-medium text-slate-700">
                  Enter 6-digit OTP
                </label>
                <input
                  id="otpCode"
                  type="text"
                  maxLength={6}
                  inputMode="numeric"
                  autoComplete="one-time-code"
                  className="mx-auto block w-full max-w-[220px] rounded-lg border border-slate-300 px-3 py-2.5 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none ring-blue-600 transition focus:border-blue-600 focus:ring-1"
                  placeholder="000000"
                  {...register('otpCode', {
                    required: 'OTP is required',
                    pattern: {
                      value: /^\d{6}$/,
                      message: 'OTP must be 6 digits',
                    },
                  })}
                />
                {errors.otpCode ? (
                  <p className="mt-1 text-center text-xs font-medium text-red-600">{errors.otpCode.message}</p>
                ) : null}
              </div>
            ) : null}

            <button
              type="button"
              onClick={handleSendOtp}
              disabled={isSendingOtp || countdown > 0}
              className="w-full rounded-lg border border-blue-200 bg-blue-50 px-4 py-2.5 text-sm font-semibold text-blue-700 transition hover:bg-blue-100 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSendingOtp
                ? 'Sending OTP...'
                : `${hasSentOtp ? 'Resend OTP' : 'Send OTP'}${countdown > 0 ? ` (${countdown}s)` : ''}`}
            </button>

            {otpMessage ? (
              <p className="rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">
                {otpMessage}
              </p>
            ) : null}

            {sendOtpError ? (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                {sendOtpError}
              </p>
            ) : null}

            {submitError ? (
              <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                {submitError}
              </p>
            ) : null}

            {successMessage ? (
              <p className="rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">
                {successMessage}
              </p>
            ) : null}

            <button
              type="submit"
              disabled={isSubmitting || !hasSentOtp || String(otpCodeValue || '').length !== 6}
              className="w-full rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-70"
            >
              {isSubmitting ? 'Creating account...' : 'Sign Up'}
            </button>
          </form>

          <p className="mt-6 text-center text-sm text-slate-600">
            Already have an account?{' '}
            <Link to="/login" className="font-semibold text-blue-600 hover:text-blue-700">
              Login
            </Link>
          </p>
        </div>
      </div>
    </section>
  )
}

export default SignupPage
