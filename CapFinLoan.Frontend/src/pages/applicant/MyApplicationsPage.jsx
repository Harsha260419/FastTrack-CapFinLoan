import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import StatusBadge from '../../components/StatusBadge'

const PAGE_SIZE = 10

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

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .replace(/\s+/g, '_')
    .toUpperCase()
}

function formatAmount(value) {
  const amount = Number(value)
  if (Number.isNaN(amount)) {
    return '-'
  }

  return amount.toLocaleString('en-IN')
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

function MyApplicationsPage() {
  const [applications, setApplications] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [pageNumber, setPageNumber] = useState(1)

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

  const statusOptions = useMemo(() => {
    const statuses = [...new Set(applications.map((item) => normalizeStatus(item?.status)).filter(Boolean))]
    return ['ALL', ...statuses]
  }, [applications])

  const filteredRows = useMemo(() => {
    const lowerSearch = searchTerm.trim().toLowerCase()

    return applications.filter((item) => {
      const fullName = String(item?.fullName || '').toLowerCase()
      const matchesSearch = fullName.includes(lowerSearch)
      const itemStatus = normalizeStatus(item?.status)
      const matchesStatus = statusFilter === 'ALL' || itemStatus === statusFilter
      return matchesSearch && matchesStatus
    })
  }, [applications, searchTerm, statusFilter])

  const totalPages = Math.max(1, Math.ceil(filteredRows.length / PAGE_SIZE))

  const pagedRows = useMemo(() => {
    const start = (pageNumber - 1) * PAGE_SIZE
    return filteredRows.slice(start, start + PAGE_SIZE)
  }, [filteredRows, pageNumber])

  useEffect(() => {
    setPageNumber(1)
  }, [searchTerm, statusFilter])

  useEffect(() => {
    if (pageNumber > totalPages) {
      setPageNumber(totalPages)
    }
  }, [pageNumber, totalPages])

  return (
    <section className="space-y-6">
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold tracking-tight text-slate-900">My Applications</h1>
          <p className="mt-1 text-sm text-slate-600">View and manage all your submitted applications.</p>
        </div>

        <Link
          to="/applicant/apply"
          className="inline-flex items-center justify-center rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700"
        >
          Apply New Loan
        </Link>
      </div>

      <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:grid-cols-2">
        <input
          type="text"
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          placeholder="Search by full name"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
        />

        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
          className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
        >
          {statusOptions.map((status) => (
            <option key={status} value={status}>
              {status === 'ALL' ? 'ALL' : status.replace(/_/g, ' ')}
            </option>
          ))}
        </select>
      </div>

      <div className="rounded-xl border border-slate-200 bg-white shadow-sm">
        {isLoading ? <LoadingSpinner /> : null}

        {!isLoading && errorMessage ? (
          <p className="p-6 text-sm font-medium text-red-600">{errorMessage}</p>
        ) : null}

        {!isLoading && !errorMessage && filteredRows.length === 0 ? (
          <p className="p-8 text-center text-sm text-slate-600">No applications match your filters.</p>
        ) : null}

        {!isLoading && !errorMessage && filteredRows.length > 0 ? (
          <>
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
                  {pagedRows.map((item) => {
                    const applicationId = item?.applicationId || '-'
                    const fullName = item?.fullName || '-'
                    const purpose = item?.loanPurpose || '-'
                    const normalizedStatus = normalizeStatus(item?.status)
                    const uiStatus = toUiStatus(item?.status)

                    return (
                      <tr key={String(applicationId)} className="hover:bg-slate-50/80">
                        <td className="whitespace-nowrap px-4 py-3 text-sm font-medium text-slate-900">{String(applicationId).slice(0, 8)}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{fullName}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{formatAmount(item?.loanAmount)}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{purpose}</td>
                        <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">
                          <StatusBadge status={uiStatus} />
                        </td>
                        <td className="whitespace-nowrap px-4 py-3 text-sm text-slate-700">{formatDate(item?.createdAt)}</td>
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

            <div className="flex items-center justify-between border-t border-slate-200 px-4 py-3">
              <p className="text-sm text-slate-600">Page {pageNumber} of {totalPages}</p>
              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={() => setPageNumber((prev) => Math.max(1, prev - 1))}
                  disabled={pageNumber === 1}
                  className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Previous
                </button>
                <button
                  type="button"
                  onClick={() => setPageNumber((prev) => Math.min(totalPages, prev + 1))}
                  disabled={pageNumber === totalPages}
                  className="rounded-md border border-slate-300 px-3 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  Next
                </button>
              </div>
            </div>
          </>
        ) : null}
      </div>
    </section>
  )
}

export default MyApplicationsPage
