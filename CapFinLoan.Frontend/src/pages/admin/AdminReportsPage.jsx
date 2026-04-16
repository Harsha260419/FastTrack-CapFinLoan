import { Download } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Legend,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'

function formatSubmittedDate(value) {
  if (!value) {
    return ''
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return ''
  }

  const day = String(parsed.getDate()).padStart(2, '0')
  const month = String(parsed.getMonth() + 1).padStart(2, '0')
  const year = parsed.getFullYear()
  return `${day}-${month}-${year}`
}

function buildApplicationsCsvRows(applications) {
  const rows = [
    [
      'Application ID',
      'Applicant Name',
      'Email',
      'Phone',
      'Requested Amount',
      'Tenure (Months)',
      'Status',
      'Submitted Date',
    ],
  ]

  applications.forEach((item) => {
    rows.push([
      item?.applicationId ?? '',
      item?.applicantName ?? '',
      item?.email ?? '',
      item?.phoneNumber ?? '',
      item?.loanAmount ?? '',
      item?.tenureMonths ?? '',
      item?.status ?? '',
      formatSubmittedDate(item?.createdDate),
    ])
  })

  return rows
}

function toCsvString(rows) {
  return rows
    .map((row) =>
      row
        .map((field) => {
          const value = String(field ?? '')
          if (value.includes(',') || value.includes('"') || value.includes('\n')) {
            return `"${value.replace(/"/g, '""')}"`
          }
          return value
        })
        .join(','),
    )
    .join('\n')
}

function AdminReportsPage() {
  const [dashboard, setDashboard] = useState({
    totalApplications: 0,
    submittedCount: 0,
    docsPendingCount: 0,
    docsVerifiedCount: 0,
    approvedCount: 0,
    rejectedCount: 0,
  })
  const [isLoading, setIsLoading] = useState(true)
  const [errorMessage, setErrorMessage] = useState('')
  const [isExporting, setIsExporting] = useState(false)

  useEffect(() => {
    let isMounted = true

    const loadDashboard = async () => {
      setIsLoading(true)
      setErrorMessage('')

      try {
        const response = await axiosInstance.get('/gateway/admin/dashboard')
        const payload = response?.data?.data || response?.data || {}

        if (isMounted) {
          setDashboard({
            totalApplications: Number(payload?.totalApplications) || 0,
            submittedCount: Number(payload?.submittedCount) || 0,
            docsPendingCount: Number(payload?.docsPendingCount) || 0,
            docsVerifiedCount: Number(payload?.docsVerifiedCount) || 0,
            approvedCount: Number(payload?.approvedCount) || 0,
            rejectedCount: Number(payload?.rejectedCount) || 0,
          })
        }
      } catch (error) {
        if (isMounted) {
          setErrorMessage('Unable to load reports right now.')
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

  const barData = useMemo(
    () => [
      { status: 'Submitted', count: dashboard.submittedCount },
      { status: 'Docs Pending', count: dashboard.docsPendingCount },
      { status: 'Docs Verified', count: dashboard.docsVerifiedCount },
      { status: 'Approved', count: dashboard.approvedCount },
      { status: 'Rejected', count: dashboard.rejectedCount },
    ],
    [dashboard],
  )

  const pieData = useMemo(() => {
    const inProgress = Math.max(
      0,
      dashboard.totalApplications - dashboard.approvedCount - dashboard.rejectedCount,
    )

    return [
      { name: 'Approved', value: dashboard.approvedCount, color: '#16a34a' },
      { name: 'Rejected', value: dashboard.rejectedCount, color: '#dc2626' },
      { name: 'In Progress', value: inProgress, color: '#2563eb' },
    ]
  }, [dashboard])

  const exportCsv = async () => {
    setIsExporting(true)

    try {
      const response = await axiosInstance.get('/gateway/admin/applications')
      const items = Array.isArray(response?.data)
        ? response.data
        : (response?.data?.items ?? [])

      const csvRows = buildApplicationsCsvRows(items)
      const csv = toCsvString(csvRows)
      const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
      const url = window.URL.createObjectURL(blob)
      const anchor = document.createElement('a')
      anchor.href = url
      anchor.download = `admin-applications-${new Date().toISOString().slice(0, 10)}.csv`
      anchor.click()
      window.URL.revokeObjectURL(url)
    } catch (error) {
      window.alert('Unable to export applications CSV right now. Please try again.')
    } finally {
      setIsExporting(false)
    }
  }

  return (
    <section className="space-y-6">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Reports</h1>
          <p className="mt-1 text-sm text-slate-600">Analyze application trends across the pipeline.</p>
        </div>

        <button
          type="button"
          onClick={exportCsv}
          disabled={isExporting}
          className="inline-flex items-center gap-2 rounded-lg bg-blue-600 px-4 py-2.5 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-70"
        >
          <Download size={16} />
          {isExporting ? 'Exporting...' : 'Export CSV'}
        </button>
      </div>

      {isLoading ? <LoadingSpinner /> : null}

      {!isLoading && errorMessage ? (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm font-medium text-red-700">{errorMessage}</p>
      ) : null}

      {!isLoading && !errorMessage ? (
        <>
          <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Total Applications</p>
              <p className="mt-2 text-3xl font-bold text-slate-900">{dashboard.totalApplications}</p>
            </article>
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Submitted</p>
              <p className="mt-2 text-3xl font-bold text-blue-600">{dashboard.submittedCount}</p>
            </article>
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Docs Pending</p>
              <p className="mt-2 text-3xl font-bold text-amber-600">{dashboard.docsPendingCount}</p>
            </article>
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Docs Verified</p>
              <p className="mt-2 text-3xl font-bold text-teal-600">{dashboard.docsVerifiedCount}</p>
            </article>
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Approved</p>
              <p className="mt-2 text-3xl font-bold text-green-600">{dashboard.approvedCount}</p>
            </article>
            <article className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <p className="text-sm text-slate-600">Rejected</p>
              <p className="mt-2 text-3xl font-bold text-red-600">{dashboard.rejectedCount}</p>
            </article>
          </div>

          <div className="grid gap-6 xl:grid-cols-2">
            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-slate-900">Applications by Status</h2>
              <div className="h-80 w-full">
                <ResponsiveContainer>
                  <BarChart data={barData}>
                    <CartesianGrid strokeDasharray="3 3" />
                    <XAxis dataKey="status" />
                    <YAxis allowDecimals={false} />
                    <Tooltip />
                    <Legend />
                    <Bar dataKey="count" name="Applications" fill="#2563eb" radius={[6, 6, 0, 0]} />
                  </BarChart>
                </ResponsiveContainer>
              </div>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-slate-900">Decision Breakdown</h2>
              <div className="h-80 w-full">
                <ResponsiveContainer>
                  <PieChart>
                    <Pie
                      data={pieData}
                      dataKey="value"
                      nameKey="name"
                      cx="50%"
                      cy="50%"
                      innerRadius={72}
                      outerRadius={110}
                      paddingAngle={2}
                    >
                      {pieData.map((entry) => (
                        <Cell key={entry.name} fill={entry.color} />
                      ))}
                    </Pie>
                    <Tooltip />
                    <Legend />
                  </PieChart>
                </ResponsiveContainer>
              </div>
            </section>
          </div>
        </>
      ) : null}
    </section>
  )
}

export default AdminReportsPage
