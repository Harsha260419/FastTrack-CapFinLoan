const statusStyles = {
  Draft: 'bg-slate-100 text-slate-700 ring-slate-300/70',
  Submitted: 'bg-blue-50 text-blue-700 ring-blue-300/70',
  'Docs Pending': 'bg-yellow-50 text-yellow-700 ring-yellow-300/70',
  'Docs Verified': 'bg-teal-50 text-teal-700 ring-teal-300/70',
  'Under Review': 'bg-purple-50 text-purple-700 ring-purple-300/70',
  Approved: 'bg-green-50 text-green-700 ring-green-300/70',
  Verified: 'bg-green-50 text-green-700 ring-green-300/70',
  Pending: 'bg-yellow-50 text-yellow-700 ring-yellow-300/70',
  Rejected: 'bg-red-50 text-red-700 ring-red-300/70',
  Closed: 'bg-slate-200 text-slate-700 ring-slate-400/70',
}

function StatusBadge({ status }) {
  const tone = statusStyles[status] || 'bg-slate-100 text-slate-700 ring-slate-300/70'

  return (
    <span
      className={`inline-flex items-center rounded-full px-2.5 py-1 text-xs font-semibold ring-1 ring-inset ${tone}`}
    >
      {status}
    </span>
  )
}

export default StatusBadge
