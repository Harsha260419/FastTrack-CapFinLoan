import { Check } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { useNavigate, useSearchParams } from 'react-router-dom'
import axiosInstance from '../api/axiosInstance'
import LoadingSpinner from '../components/LoadingSpinner'
import useAuthStore from '../store/authStore'

const STEP_TITLES = ['Personal Info', 'Employment', 'Loan Details', 'Review & Submit']
const TENURE_OPTIONS = [12, 24, 36, 48, 60, 72, 84, 96, 108, 120, 132, 144, 156, 168, 180]

const STEP_FIELDS = {
  1: [
    'firstName',
    'lastName',
    'dateOfBirth',
    'gender',
    'email',
    'phone',
    'addressLine1',
    'city',
    'state',
    'postalCode',
  ],
  2: ['employerName', 'employmentType', 'monthlyIncome', 'annualIncome'],
  3: ['requestedAmount', 'requestedTenureMonths', 'loanPurpose'],
  4: ['declarationAccepted'],
}

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .replace(/\s+/g, '_')
    .toUpperCase()
}

function toIsoDate(dateString) {
  if (!dateString) {
    return null
  }

  if (!/^\d{4}-\d{2}-\d{2}$/.test(String(dateString))) {
    return null
  }

  return `${dateString}T00:00:00Z`
}

function toInputDate(value) {
  if (!value) {
    return ''
  }

  const rawValue = String(value)
  const dateMatch = rawValue.match(/^(\d{4}-\d{2}-\d{2})/)
  if (dateMatch?.[1]) {
    return dateMatch[1]
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return ''
  }

  return parsed.toISOString().split('T')[0]
}

function getApplicationId(payload) {
  return payload?.id || payload?.applicationId || payload?.data?.id || payload?.data?.applicationId || null
}

function parseNumberWithFallback(value, fallbackValue) {
  const parsed = Number(value)
  return Number.isFinite(parsed) ? parsed : fallbackValue
}

function ApplicantApplyPage() {
  const navigate = useNavigate()
  const authEmail = useAuthStore((state) => state.email)
  const [searchParams, setSearchParams] = useSearchParams()
  const queryId = searchParams.get('id')

  const [currentStep, setCurrentStep] = useState(1)
  const [applicationId, setApplicationId] = useState(queryId || null)
  const [isLoadingApplication, setIsLoadingApplication] = useState(false)
  const [isSavingDraft, setIsSavingDraft] = useState(false)
  const [isSubmittingApplication, setIsSubmittingApplication] = useState(false)
  const [apiError, setApiError] = useState('')
  const [successMessage, setSuccessMessage] = useState('')
  const [isReadOnly, setIsReadOnly] = useState(false)
  const skipNextQueryLoadRef = useRef(false)

  const {
    register,
    handleSubmit,
    reset,
    trigger,
    watch,
    formState: { errors },
  } = useForm({
    defaultValues: {
      firstName: '',
      lastName: '',
      dateOfBirth: '',
      gender: 'Male',
      email: authEmail || '',
      phone: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: '',
      postalCode: '',
      employerName: '',
      employmentType: 'Salaried',
      monthlyIncome: '',
      annualIncome: '',
      requestedAmount: '',
      requestedTenureMonths: '12',
      loanPurpose: 'Home',
      remarks: '',
      declarationAccepted: false,
      comments: '',
    },
  })

  const isBusy = isLoadingApplication || isSavingDraft || isSubmittingApplication
  const values = watch()

  useEffect(() => {
    if (!queryId) {
      return
    }

    if (skipNextQueryLoadRef.current && String(applicationId || '') === String(queryId)) {
      skipNextQueryLoadRef.current = false
      return
    }

    let isMounted = true

    const loadApplicationForEdit = async () => {
      setIsLoadingApplication(true)
      setApiError('')
      setSuccessMessage('')

      try {
        const response = await axiosInstance.get(`/gateway/applications/${queryId}`)
        const payload = response?.data || {}

        if (!isMounted) {
          return
        }

        const fetchedId = getApplicationId(payload) || queryId
        const status = String(payload?.status || '').trim()
        const blocked = Boolean(status && status !== 'Draft')

        const personal = payload?.personalDetails || {}
        const employment = payload?.employmentDetails || {}
        const loan = payload?.loanDetails || {}

        setApplicationId(String(fetchedId))
        setIsReadOnly(blocked)

        if (blocked) {
          setApiError('This application can no longer be edited')
        }

        reset({
          firstName: personal?.firstName || '',
          lastName: personal?.lastName || '',
          dateOfBirth: toInputDate(personal?.dateOfBirth),
          gender: personal?.gender || 'Male',
          email: personal?.email || authEmail || '',
          phone: personal?.phone || '',
          addressLine1: personal?.addressLine1 || '',
          addressLine2: personal?.addressLine2 || '',
          city: personal?.city || '',
          state: personal?.state || '',
          postalCode: personal?.postalCode || '',
          employerName: employment?.employerName || '',
          employmentType: employment?.employmentType || 'Salaried',
          monthlyIncome: employment?.monthlyIncome ?? '',
          annualIncome: employment?.annualIncome ?? '',
          requestedAmount: loan?.requestedAmount ?? '',
          requestedTenureMonths: String(loan?.requestedTenureMonths || '12'),
          loanPurpose: loan?.loanPurpose || 'Home',
          remarks: loan?.remarks || '',
          declarationAccepted: Boolean(payload?.declarationAccepted),
          comments: payload?.comments || '',
        })
      } catch (error) {
        if (isMounted) {
          setApiError('Failed to load application details. Please try again.')
        }
      } finally {
        if (isMounted) {
          setIsLoadingApplication(false)
        }
      }
    }

    loadApplicationForEdit()

    return () => {
      isMounted = false
    }
  }, [queryId, reset, authEmail, applicationId])

  const draftPayload = useMemo(() => ({
    personalDetails: {
      firstName: values.firstName,
      lastName: values.lastName,
      dateOfBirth: toIsoDate(values.dateOfBirth),
      gender: values.gender,
      email: values.email,
      phone: values.phone,
      addressLine1: values.addressLine1,
      addressLine2: values.addressLine2 || '',
      city: values.city,
      state: values.state,
      postalCode: values.postalCode,
    },
    employmentDetails: {
      employerName: values.employerName,
      employmentType: values.employmentType,
      monthlyIncome: parseNumberWithFallback(values.monthlyIncome, 0),
      annualIncome: parseNumberWithFallback(values.annualIncome, 0),
    },
    loanDetails: {
      requestedAmount: parseNumberWithFallback(values.requestedAmount, 10000),
      requestedTenureMonths: parseNumberWithFallback(values.requestedTenureMonths, 12),
      loanPurpose: values.loanPurpose || 'Home',
      remarks: values.remarks || null,
    },
  }), [values])

  const handleNext = async () => {
    if (isReadOnly) {
      return
    }

    const isValid = await trigger(STEP_FIELDS[currentStep])
    if (!isValid) {
      return
    }

    setCurrentStep((prev) => Math.min(prev + 1, 4))
  }

  const handlePrevious = () => {
    setCurrentStep((prev) => Math.max(prev - 1, 1))
  }

  const handleSaveDraft = async () => {
    if (isReadOnly) {
      return
    }

    const fieldsToValidate = Array.from(new Set([
      ...STEP_FIELDS[1],
      ...(currentStep >= 2 ? STEP_FIELDS[2] : []),
      ...(currentStep >= 3 ? STEP_FIELDS[3] : []),
    ]))

    const isValid = await trigger(fieldsToValidate)
    if (!isValid) {
      return
    }

    setIsSavingDraft(true)
    setApiError('')
    setSuccessMessage('')

    try {
      if (!applicationId) {
        const createResponse = await axiosInstance.post('/gateway/applications', draftPayload)
        const createdId = getApplicationId(createResponse?.data)

        if (createdId) {
          const createdIdString = String(createdId)
          setApplicationId(createdIdString)
          skipNextQueryLoadRef.current = true
          setSearchParams({ id: createdIdString }, { replace: true })
        }
      } else {
        await axiosInstance.put(`/gateway/applications/${applicationId}`, draftPayload)
      }

      setSuccessMessage('Draft saved successfully')
    } catch (error) {
      const apiMessage = error?.response?.data?.message
      setApiError(apiMessage || 'Failed to save draft. Please try again.')
    } finally {
      setIsSavingDraft(false)
    }
  }

  const onSubmit = async (formValues) => {
    if (isReadOnly) {
      return
    }

    setIsSubmittingApplication(true)
    setApiError('')
    setSuccessMessage('')

    try {
      let resolvedId = applicationId

      if (!resolvedId) {
        const createResponse = await axiosInstance.post('/gateway/applications', draftPayload)
        const createdId = getApplicationId(createResponse?.data)

        if (!createdId) {
          throw new Error('Unable to create draft before submit')
        }

        resolvedId = String(createdId)
        setApplicationId(resolvedId)
        setSearchParams({ id: resolvedId })
      }

      await axiosInstance.post(`/gateway/applications/${resolvedId}/submit`, {
        declarationAccepted: true,
        comments: formValues.comments || null,
      })

      navigate('/applicant/dashboard')
    } catch (error) {
      setApiError('Failed to submit application. Please try again.')
    } finally {
      setIsSubmittingApplication(false)
    }
  }

  return (
    <section className="mx-auto w-full max-w-6xl">
      <div className="overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-sm">
        <div className="sticky top-0 z-10 border-b border-slate-200 bg-white px-5 py-4 sm:px-8">
          <ol className="grid grid-cols-2 gap-3 md:grid-cols-4">
            {STEP_TITLES.map((title, index) => {
              const stepNumber = index + 1
              const isActive = currentStep === stepNumber
              const isCompleted = currentStep > stepNumber

              return (
                <li key={title} className="flex items-center gap-2">
                  <span
                    className={`inline-flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold ${
                      isCompleted
                        ? 'bg-green-100 text-green-700'
                        : isActive
                          ? 'bg-blue-600 text-white'
                          : 'bg-slate-200 text-slate-700'
                    }`}
                  >
                    {isCompleted ? <Check size={14} /> : stepNumber}
                  </span>
                  <span className={`text-xs font-medium ${isActive ? 'text-blue-700' : 'text-slate-600'}`}>
                    {title}
                  </span>
                </li>
              )
            })}
          </ol>
        </div>

        <form onSubmit={handleSubmit(onSubmit)} className="px-5 py-6 sm:px-8">
          {isBusy ? <LoadingSpinner /> : null}

          {!isBusy ? (
            <div className="space-y-6">
              {apiError ? (
                <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{apiError}</p>
              ) : null}

              {successMessage ? (
                <p className="rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">{successMessage}</p>
              ) : null}

              {currentStep === 1 ? (
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">First Name</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('firstName', { required: 'First name is required' })}
                    />
                    {errors.firstName ? <p className="mt-1 text-xs text-red-600">{errors.firstName.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Last Name</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('lastName', { required: 'Last name is required' })}
                    />
                    {errors.lastName ? <p className="mt-1 text-xs text-red-600">{errors.lastName.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Date of Birth</label>
                    <input
                      type="date"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('dateOfBirth', { required: 'Date of birth is required' })}
                    />
                    {errors.dateOfBirth ? <p className="mt-1 text-xs text-red-600">{errors.dateOfBirth.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Gender</label>
                    <select
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('gender')}
                    >
                      <option>Male</option>
                      <option>Female</option>
                      <option>Other</option>
                    </select>
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Email</label>
                    <input
                      type="email"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('email', {
                        required: 'Email is required',
                        pattern: {
                          value: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
                          message: 'Enter a valid email',
                        },
                      })}
                    />
                    {errors.email ? <p className="mt-1 text-xs text-red-600">{errors.email.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Phone</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('phone', { required: 'Phone is required' })}
                    />
                    {errors.phone ? <p className="mt-1 text-xs text-red-600">{errors.phone.message}</p> : null}
                  </div>

                  <div className="sm:col-span-2">
                    <label className="mb-1 block text-sm font-medium text-slate-700">Address Line 1</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('addressLine1', { required: 'Address line 1 is required' })}
                    />
                    {errors.addressLine1 ? <p className="mt-1 text-xs text-red-600">{errors.addressLine1.message}</p> : null}
                  </div>

                  <div className="sm:col-span-2">
                    <label className="mb-1 block text-sm font-medium text-slate-700">Address Line 2</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('addressLine2')}
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">City</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('city', { required: 'City is required' })}
                    />
                    {errors.city ? <p className="mt-1 text-xs text-red-600">{errors.city.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">State</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('state', { required: 'State is required' })}
                    />
                    {errors.state ? <p className="mt-1 text-xs text-red-600">{errors.state.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Postal Code</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('postalCode', { required: 'Postal code is required' })}
                    />
                    {errors.postalCode ? <p className="mt-1 text-xs text-red-600">{errors.postalCode.message}</p> : null}
                  </div>
                </div>
              ) : null}

              {currentStep === 2 ? (
                <div className="grid gap-4 sm:grid-cols-2">
                  <div className="sm:col-span-2">
                    <label className="mb-1 block text-sm font-medium text-slate-700">Employer Name</label>
                    <input
                      type="text"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('employerName', { required: 'Employer name is required' })}
                    />
                    {errors.employerName ? <p className="mt-1 text-xs text-red-600">{errors.employerName.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Employment Type</label>
                    <select
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('employmentType', { required: 'Employment type is required' })}
                    >
                      <option>Salaried</option>
                      <option>Self-Employed</option>
                      <option>Business</option>
                    </select>
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Monthly Income</label>
                    <input
                      type="number"
                      min="5000"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('monthlyIncome', {
                        required: 'Monthly income is required',
                        min: { value: 5000, message: 'Minimum monthly income is 5000' },
                      })}
                    />
                    {errors.monthlyIncome ? <p className="mt-1 text-xs text-red-600">{errors.monthlyIncome.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Annual Income</label>
                    <input
                      type="number"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('annualIncome', { required: 'Annual income is required' })}
                    />
                    {errors.annualIncome ? <p className="mt-1 text-xs text-red-600">{errors.annualIncome.message}</p> : null}
                  </div>
                </div>
              ) : null}

              {currentStep === 3 ? (
                <div className="grid gap-4 sm:grid-cols-2">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Requested Amount</label>
                    <input
                      type="number"
                      min="10000"
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('requestedAmount', {
                        required: 'Requested amount is required',
                        min: { value: 10000, message: 'Minimum requested amount is 10000' },
                      })}
                    />
                    {errors.requestedAmount ? <p className="mt-1 text-xs text-red-600">{errors.requestedAmount.message}</p> : null}
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Tenure in Months</label>
                    <select
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('requestedTenureMonths', { required: 'Tenure is required' })}
                    >
                      {TENURE_OPTIONS.map((months) => (
                        <option key={months} value={months}>
                          {months}
                        </option>
                      ))}
                    </select>
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Loan Purpose</label>
                    <select
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('loanPurpose', { required: 'Loan purpose is required' })}
                    >
                      <option>Home</option>
                      <option>Car</option>
                      <option>Education</option>
                      <option>Personal</option>
                      <option>Business</option>
                    </select>
                  </div>

                  <div className="sm:col-span-2">
                    <label className="mb-1 block text-sm font-medium text-slate-700">Remarks</label>
                    <textarea
                      rows={4}
                      disabled={isReadOnly}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                      {...register('remarks')}
                    />
                  </div>
                </div>
              ) : null}

              {currentStep === 4 ? (
                <div className="space-y-4">
                  <section className="rounded-lg border border-slate-200 p-4">
                    <h2 className="mb-2 text-sm font-semibold text-slate-900">Personal</h2>
                    <dl className="grid grid-cols-1 gap-2 text-sm text-slate-700 sm:grid-cols-2">
                      <div><dt className="font-medium">Name</dt><dd>{values.firstName} {values.lastName}</dd></div>
                      <div><dt className="font-medium">Date of Birth</dt><dd>{values.dateOfBirth || '-'}</dd></div>
                      <div><dt className="font-medium">Gender</dt><dd>{values.gender || '-'}</dd></div>
                      <div><dt className="font-medium">Email</dt><dd>{values.email || '-'}</dd></div>
                      <div><dt className="font-medium">Phone</dt><dd>{values.phone || '-'}</dd></div>
                      <div><dt className="font-medium">City / State</dt><dd>{values.city || '-'} / {values.state || '-'}</dd></div>
                      <div className="sm:col-span-2"><dt className="font-medium">Address</dt><dd>{values.addressLine1 || '-'} {values.addressLine2 || ''}</dd></div>
                      <div><dt className="font-medium">Postal Code</dt><dd>{values.postalCode || '-'}</dd></div>
                    </dl>
                  </section>

                  <section className="rounded-lg border border-slate-200 p-4">
                    <h2 className="mb-2 text-sm font-semibold text-slate-900">Employment</h2>
                    <dl className="grid grid-cols-1 gap-2 text-sm text-slate-700 sm:grid-cols-2">
                      <div><dt className="font-medium">Employer Name</dt><dd>{values.employerName || '-'}</dd></div>
                      <div><dt className="font-medium">Employment Type</dt><dd>{values.employmentType || '-'}</dd></div>
                      <div><dt className="font-medium">Monthly Income</dt><dd>{values.monthlyIncome || '-'}</dd></div>
                      <div><dt className="font-medium">Annual Income</dt><dd>{values.annualIncome || '-'}</dd></div>
                    </dl>
                  </section>

                  <section className="rounded-lg border border-slate-200 p-4">
                    <h2 className="mb-2 text-sm font-semibold text-slate-900">Loan</h2>
                    <dl className="grid grid-cols-1 gap-2 text-sm text-slate-700 sm:grid-cols-2">
                      <div><dt className="font-medium">Requested Amount</dt><dd>{values.requestedAmount || '-'}</dd></div>
                      <div><dt className="font-medium">Tenure</dt><dd>{values.requestedTenureMonths || '-'} months</dd></div>
                      <div><dt className="font-medium">Purpose</dt><dd>{values.loanPurpose || '-'}</dd></div>
                      <div><dt className="font-medium">Remarks</dt><dd>{values.remarks || '-'}</dd></div>
                    </dl>
                  </section>

                  <section className="rounded-lg border border-slate-200 p-4">
                    <label className="inline-flex items-start gap-2 text-sm text-slate-700">
                      <input
                        type="checkbox"
                        disabled={isReadOnly}
                        className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-600 disabled:bg-slate-100"
                        {...register('declarationAccepted', {
                          required: 'You must accept the declaration before submitting',
                        })}
                      />
                      <span>I accept the declaration and confirm all information is accurate</span>
                    </label>
                    {errors.declarationAccepted ? (
                      <p className="mt-1 text-xs text-red-600">{errors.declarationAccepted.message}</p>
                    ) : null}

                    <div className="mt-3">
                      <label className="mb-1 block text-sm font-medium text-slate-700">Comments</label>
                      <textarea
                        rows={3}
                        disabled={isReadOnly}
                        className="w-full rounded-lg border border-slate-300 px-3 py-2.5 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600 disabled:bg-slate-100"
                        {...register('comments')}
                      />
                    </div>
                  </section>
                </div>
              ) : null}

              <div className="flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-4">
                <div>
                  {currentStep === 3 ? (
                    <button
                      type="button"
                      disabled={isReadOnly}
                      onClick={handleSaveDraft}
                      className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Save Draft
                    </button>
                  ) : null}
                </div>

                <div className="ml-auto flex items-center gap-2">
                  <button
                    type="button"
                    onClick={handlePrevious}
                    disabled={currentStep === 1 || isReadOnly}
                    className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Previous
                  </button>

                  {currentStep < 4 ? (
                    <button
                      type="button"
                      disabled={isReadOnly}
                      onClick={handleNext}
                      className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Next
                    </button>
                  ) : (
                    <button
                      type="submit"
                      disabled={isReadOnly}
                      className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Submit
                    </button>
                  )}
                </div>
              </div>
            </div>
          ) : null}
        </form>
      </div>
    </section>
  )
}

export default ApplicantApplyPage
