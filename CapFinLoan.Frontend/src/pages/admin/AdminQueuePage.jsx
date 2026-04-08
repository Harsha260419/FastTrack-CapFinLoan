import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import StatusBadge from '../../components/StatusBadge'

const PAGE_SIZE = 10
const STATUS_OPTIONS = [
  { value: 'ALL', label: 'ALL' },
  { value: 'DRAFT', label: 'DRAFT' },
  { value: 'SUBMITTED', label: 'SUBMITTED' },
  { value: 'DOCS_PENDING', label: 'DOCS PENDING' },
  { value: 'DOCS_VERIFIED', label: 'DOCS VERIFIED' },
  { value: 'UNDER_REVIEW', label: 'UNDER REVIEW' },
  { value: 'APPROVED', label: 'APPROVED' },
  { value: 'REJECTED', label: 'REJECTED' },
]

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/\s+/g, '_')
    .toUpperCase()
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

function AdminQueuePage() {
  const [applications, setApplications] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [searchTerm, setSearchTerm] = useState('')
  const [statusFilter, setStatusFilter] = useState('ALL')
  const [pageNumber, setPageNumber] = useState(1)

  useEffect(() => {
    let isMounted = true

    const loadQueue = async () => {
      setIsLoading(true)
      setErrorMessage('')

      try {
        const response = await axiosInstance.get(`/gateway/admin/applications?t=${Date.now()}`)
        const payload = response?.data
        const rows = Array.isArray(payload)
          ? payload
          : Array.isArray(payload?.items)
            ? payload.items
            : Array.isArray(payload?.data)
              ? payload.data
              : []

        if (isMounted) {
          setApplications(rows)
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage('Unable to load applications queue right now.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadQueue()

    return () => {
      isMounted = false
    }
  }, [])

  const filteredRows = useMemo(() => {
    const bySearch = applications.filter((item) => {
      const applicantName = String(item?.fullName || item?.applicantName || '').toLowerCase()
      return applicantName.includes(searchTerm.trim().toLowerCase())
    })

    if (statusFilter === 'ALL') {
      return bySearch
    }

    return bySearch.filter((item) => normalizeStatus(item?.status) === statusFilter)
  }, [applications, searchTerm, statusFilter])

  const totalPages = Math.max(1, Math.ceil(filteredRows.length / PAGE_SIZE))

  const pagedRows = useMemo(() => {
    const startIndex = (pageNumber - 1) * PAGE_SIZE
    return filteredRows.slice(startIndex, startIndex + PAGE_SIZE)
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
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Applications Queue</h1>
        <p className="mt-1 text-sm text-slate-600">Search, filter, and review loan applications.</p>
      </div>

      <div className="grid gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:grid-cols-2">
        <input
          type="text"
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          placeholder="Search by applicant name"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
        />

        <select
          value={statusFilter}
          onChange={(event) => setStatusFilter(event.target.value)}
          className="w-full rounded-lg border border-slate-300 bg-white px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
        >
          {STATUS_OPTIONS.map((statusOption) => (
            <option key={statusOption.value} value={statusOption.value}>
              {statusOption.label}
            </option>
          ))}
        </select>
      </div>

      <section className="rounded-xl border border-slate-200 bg-white shadow-sm">
        {isLoading ? <LoadingSpinner /> : null}

        {!isLoading && errorMessage ? (
          <p className="p-6 text-sm font-medium text-red-600">{errorMessage}</p>
        ) : null}

        {!isLoading && !errorMessage ? (
          <>
            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-slate-200">
                <thead className="bg-slate-50">
                  <tr>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Application ID</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Applicant Name</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Loan Amount</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Status</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Created Date</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Actions</th>
                  </tr>
                </thead>

                <tbody className="divide-y divide-slate-100">
                  {pagedRows.map((item) => {
                    const applicationId = String(item?.applicationId || item?.id || '-')
                    return (
                      <tr key={applicationId} className="hover:bg-slate-50/80">
                        <td className="px-4 py-3 text-sm font-medium text-slate-900">{applicationId.slice(0, 8)}</td>
                        <td className="px-4 py-3 text-sm text-slate-700">{item?.fullName || item?.applicantName || '-'}</td>
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

                  {pagedRows.length === 0 ? (
                    <tr>
                      <td colSpan={6} className="px-4 py-8 text-center text-sm text-slate-600">
                        No applications match your filters.
                      </td>
                    </tr>
                  ) : null}
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
      </section>
    </section>
  )
}

export default AdminQueuePage
