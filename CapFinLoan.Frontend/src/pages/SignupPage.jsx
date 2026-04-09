import { CheckCircle, Mail } from 'lucide-react'
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
              <h1 className="text-5xl font-bold leading-tight text-white">Start your loan journey today</h1>

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
              <h2 className="text-3xl font-bold tracking-tight text-slate-900">Create Account</h2>
              <p className="mt-2 text-sm text-slate-600">Set up your account to start applying for loans.</p>
            </div>

          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4" noValidate>
            <div>
              <label htmlFor="fullName" className="mb-1 block text-sm font-medium text-slate-700">
                Full Name
              </label>
              <input
                id="fullName"
                type="text"
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
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
              <label htmlFor="phoneNumber" className="mb-1 block text-sm font-medium text-slate-700">
                Phone Number
              </label>
              <input
                id="phoneNumber"
                type="text"
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
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
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
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
                className="w-full rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
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
                  className="mx-auto block w-full max-w-[220px] rounded-xl border border-slate-300 bg-white px-3 py-2.5 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none ring-blue-200 transition focus:border-blue-500 focus:ring-2"
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
              className="w-full rounded-xl border border-slate-900 bg-white px-4 py-2.5 text-sm font-semibold text-slate-900 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-70"
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
              className="w-full rounded-xl bg-slate-900 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-70"
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
      </div>
    </section>
  )
}

export default SignupPage
