import { CheckCircle2, Clock3, FileText, Hourglass, Send, XCircle } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import StatusBadge from '../../components/StatusBadge'

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

function formatAmount(value) {
  const amount = Number(value)
  if (Number.isNaN(amount)) {
    return '-'
  }

  return amount.toLocaleString('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  })
}

function AdminDashboardPage() {
  const [dashboard, setDashboard] = useState(null)
  const [applications, setApplications] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')

  useEffect(() => {
    let isMounted = true

    const loadDashboard = async () => {
      setIsLoading(true)
      setErrorMessage('')

      try {
        const [dashboardResponse, applicationsResponse] = await Promise.all([
          axiosInstance.get('/gateway/admin/dashboard'),
          axiosInstance.get('/gateway/admin/applications'),
        ])

        const dashboardPayload = dashboardResponse?.data?.data || dashboardResponse?.data || {}
        const applicationsPayload = applicationsResponse?.data
        const rows = Array.isArray(applicationsPayload)
          ? applicationsPayload
          : Array.isArray(applicationsPayload?.items)
            ? applicationsPayload.items
            : Array.isArray(applicationsPayload?.data)
              ? applicationsPayload.data
              : []

        if (isMounted) {
          setDashboard(dashboardPayload)
          setApplications(rows.slice(0, 5))
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage('Unable to load dashboard data right now.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadDashboard()

    return () => {
      isMounted = false
    }
  }, [])

  const kpis = useMemo(
    () => [
      {
        label: 'Total Applications',
        value: dashboard?.totalApplications ?? 0,
        icon: FileText,
        tone: 'text-slate-900',
      },
      {
        label: 'Submitted',
        value: dashboard?.submittedCount ?? 0,
        icon: Send,
        tone: 'text-blue-600',
      },
      {
        label: 'Docs Pending',
        value: dashboard?.docsPendingCount ?? 0,
        icon: Hourglass,
        tone: 'text-yellow-600',
      },
      {
        label: 'Docs Verified',
        value: dashboard?.docsVerifiedCount ?? 0,
        icon: Clock3,
        tone: 'text-teal-600',
      },
      {
        label: 'Approved',
        value: dashboard?.approvedCount ?? 0,
        icon: CheckCircle2,
        tone: 'text-green-600',
      },
      {
        label: 'Rejected',
        value: dashboard?.rejectedCount ?? 0,
        icon: XCircle,
        tone: 'text-red-600',
      },
    ],
    [dashboard],
  )

  return (
    <section className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Admin Dashboard</h1>
        <p className="mt-1 text-sm text-slate-600">Monitor application pipeline and recent submissions.</p>
      </div>

      {isLoading ? <LoadingSpinner /> : null}

      {!isLoading && errorMessage ? (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm font-medium text-red-700">{errorMessage}</p>
      ) : null}

      {!isLoading && !errorMessage ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            {kpis.map((kpi) => {
              const Icon = kpi.icon
              return (
                <article key={kpi.label} className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
                  <div className="flex items-center justify-between">
                    <p className="text-sm text-slate-600">{kpi.label}</p>
                    <Icon size={18} className={kpi.tone} />
                  </div>
                  <p className={`mt-2 text-3xl font-bold ${kpi.tone}`}>{kpi.value}</p>
                </article>
              )
            })}
          </div>

          <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
            <div className="border-b border-slate-200 px-4 py-3">
              <h2 className="text-lg font-semibold text-slate-900">Recent Applications</h2>
            </div>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Applicant Name</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Loan Amount</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Status</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Created Date</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Actions</th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-100">
                  {applications.map((item) => {
                    const applicationId = item?.applicationId || item?.id
                    const applicantName = item?.fullName || item?.applicantName || '-'
                    return (
                      <tr key={String(applicationId)}>
                        <td className="px-4 py-3 text-sm text-slate-700">{applicantName}</td>
                        <td className="px-4 py-3 text-sm text-slate-700">{formatAmount(item?.loanAmount)}</td>
                        <td className="px-4 py-3 text-sm text-slate-700">
                          <StatusBadge status={item?.status || '-'} />
                        </td>
                        <td className="px-4 py-3 text-sm text-slate-700">{formatDate(item?.createdAt || item?.createdDate)}</td>
                        <td className="px-4 py-3 text-sm">
                          <Link
                            to={`/admin/review/${applicationId}`}
                            className="rounded-md border border-blue-200 px-2.5 py-1.5 text-xs font-semibold text-blue-700 transition hover:bg-blue-50"
                          >
                            Review
                          </Link>
                        </td>
                      </tr>
                    )
                  })}

                  {applications.length === 0 ? (
                    <tr>
                      <td colSpan={5} className="px-4 py-8 text-center text-sm text-slate-600">
                        No recent applications available.
                      </td>
                    </tr>
                  ) : null}
                </tbody>
              </table>
            </div>
          </section>
        </>
      ) : null}
    </section>
  )
}

export default AdminDashboardPage
