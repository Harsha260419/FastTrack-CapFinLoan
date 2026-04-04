import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import StatusBadge from '../../components/StatusBadge'

const FINAL_STATUSES = new Set(['APPROVED', 'REJECTED', 'CLOSED'])

function toUiStatus(value) {
  const normalized = String(value || '')
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .replace(/_/g, ' ')
    .toLowerCase()

  const mapping = {
    draft: 'Draft',
    submitted: 'Submitted',
    'docs pending': 'Docs Pending',
    'docs verified': 'Docs Verified',
    'under review': 'Under Review',
    approved: 'Approved',
    rejected: 'Rejected',
    closed: 'Closed',
  }

  return mapping[normalized] || 'Draft'
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleDateString()
}

function DashboardPage() {
  const [applications, setApplications] = useState([])
  const [totalCount, setTotalCount] = useState(0)
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    let isMounted = true

    const loadApplications = async () => {
      setIsLoading(true)
      setErrorMessage('')

      try {
        const response = await axiosInstance.get(`/gateway/applications/my?t=${Date.now()}`)
        const payload = response?.data || {}
        const rows = Array.isArray(payload?.items) ? payload.items : []

        if (isMounted) {
          setApplications(rows)
          setTotalCount(Number(payload?.totalCount) || rows.length)
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage('Unable to load your applications right now.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadApplications()

    return () => {
      isMounted = false
    }
  }, [])

  const metrics = useMemo(() => {
    const total = totalCount
    const approved = applications.filter((item) => String(item?.status || '').toUpperCase() === 'APPROVED').length
    const pendingActive = applications.filter((item) => {
      const status = String(item?.status || '').toUpperCase().replace(/\s+/g, '_')
      return !FINAL_STATUSES.has(status)
    }).length

    return { total, pendingActive, approved }
  }, [applications, totalCount])

  return (
    <section className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">My Applications</h1>
          <p className="mt-1 text-sm text-slate-600">Track your loan applications and status updates.</p>
        </div>

        <Link
          to="/applicant/apply"
          className="inline-flex items-center justify-center rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700"
        >
          Apply New Loan
        </Link>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm text-slate-600">Total Applications</p>
          <p className="mt-2 text-3xl font-bold text-slate-900">{metrics.total}</p>
        </article>
        <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm text-slate-600">Pending / Active</p>
          <p className="mt-2 text-3xl font-bold text-amber-600">{metrics.pendingActive}</p>
        </article>
        <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
          <p className="text-sm text-slate-600">Approved</p>
          <p className="mt-2 text-3xl font-bold text-green-600">{metrics.approved}</p>
        </article>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
        {isLoading ? <LoadingSpinner /> : null}

        {!isLoading && errorMessage ? (
          <p className="p-6 text-sm font-medium text-red-600">{errorMessage}</p>
        ) : null}

        {!isLoading && !errorMessage && applications.length === 0 ? (
          <p className="p-8 text-center text-sm text-slate-600">No applications found yet. Start by applying for a new loan.</p>
        ) : null}

        {!isLoading && !errorMessage && applications.length > 0 ? (
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-slate-200">
              <thead className="bg-slate-50">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Application ID</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Full Name</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Loan Amount</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Purpose</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Status</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Created Date</th>
                  <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Actions</th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100">
                {applications.map((item) => {
                  const applicationId = item?.applicationId || '-'
                  const fullName = item?.fullName || item?.name || `${item?.firstName || ''} ${item?.lastName || ''}`.trim() || '-'
                  const amount = item?.loanAmount ?? item?.amount ?? '-'
                  const purpose = item?.purpose || item?.loanPurpose || '-'
                  const uiStatus = toUiStatus(item?.status)
                  const normalizedStatus = String(item?.status || '').trim().replace(/\s+/g, '_').toUpperCase()
                  const createdDate = item?.createdDate || item?.createdAt
                  const shortenedId = String(applicationId).slice(0, 8)

                  return (
                    <tr key={String(applicationId)} className="hover:bg-slate-50/80">
                      <td className="whitespace-nowrap px-4 py-3 text-sm font-medium text-slate-900">{shortenedId}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{fullName}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">
                        {typeof amount === 'number' ? amount.toLocaleString() : amount}
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{purpose}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">
                        <StatusBadge status={uiStatus} />
                      </td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{formatDate(createdDate)}</td>
                      <td className="whitespace-nowrap px-4 py-3 text-sm">
                        <div className="flex items-center gap-2">
                          <Link
                            to={`/applicant/application/${applicationId}`}
                            className="rounded-md border border-blue-200 px-2.5 py-1.5 text-xs font-semibold text-blue-700 transition hover:bg-blue-50"
                          >
                            View Details
                          </Link>
                          {normalizedStatus === 'DRAFT' ? (
                            <Link
                              to={`/applicant/apply?id=${applicationId}`}
                              className="rounded-md border border-amber-200 px-2.5 py-1.5 text-xs font-semibold text-amber-700 transition hover:bg-amber-50"
                            >
                              Edit
                            </Link>
                          ) : null}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        ) : null}
      </div>
    </section>
  )
}

export default DashboardPage