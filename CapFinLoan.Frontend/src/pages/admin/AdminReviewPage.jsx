import { Download, X } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import Modal from '../../components/Modal'
import StatusBadge from '../../components/StatusBadge'

const DOC_TYPE_CONFIG = [
  { label: 'ID Proof', apiValue: 'ID_PROOF' },
  { label: 'Address Proof', apiValue: 'ADDRESS_PROOF' },
  { label: 'Bank Statement', apiValue: 'BANK_STATEMENT' },
  { label: 'Income Proof', apiValue: 'INCOME_PROOF' },
]

function normalizeDocType(documentType) {
  const normalized = String(documentType || '').trim().toUpperCase()
  const map = {
    IDPROOF: 'ID_PROOF',
    ADDRESSPROOF: 'ADDRESS_PROOF',
    BANKSTATEMENT: 'BANK_STATEMENT',
    INCOMEPROOF: 'INCOME_PROOF',
    ID_PROOF: 'ID_PROOF',
    ADDRESS_PROOF: 'ADDRESS_PROOF',
    BANK_STATEMENT: 'BANK_STATEMENT',
    INCOME_PROOF: 'INCOME_PROOF',
  }

  return map[normalized] || normalized
}

function normalizeStatus(value) {
  return String(value || '')
    .trim()
    .replace(/([a-z0-9])([A-Z])/g, '$1_$2')
    .replace(/\s+/g, '_')
    .toUpperCase()
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  const dateValue = String(value)
  const parsed = new Date(dateValue + (dateValue.endsWith('Z') ? '' : 'Z'))
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleString('en-IN', {
    timeZone: 'Asia/Kolkata',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    hour12: true,
  })
}

function formatCurrency(value) {
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

function getDocId(document) {
  return document?.id || document?.documentId || null
}

function formatStatusLabel(status) {
  return String(status || '-').replace(/_/g, ' ')
}

function AdminReviewPage() {
  const { id } = useParams()
  const navigate = useNavigate()

  const [application, setApplication] = useState(null)
  const [historyRows, setHistoryRows] = useState([])
  const [documents, setDocuments] = useState([])
  const [documentsLoadError, setDocumentsLoadError] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [documentActionError, setDocumentActionError] = useState('')

  const [verifyLoadingByType, setVerifyLoadingByType] = useState({})
  const [rejectOpenByType, setRejectOpenByType] = useState({})
  const [rejectRemarksByType, setRejectRemarksByType] = useState({})
  const [messageByType, setMessageByType] = useState({})

  const [decisionRemarks, setDecisionRemarks] = useState('')
  const [sanctionAmount, setSanctionAmount] = useState('')
  const [interestRate, setInterestRate] = useState('10.5')
  const [decisionError, setDecisionError] = useState('')
  const [decisionSuccess, setDecisionSuccess] = useState('')
  const [decisionModal, setDecisionModal] = useState({ open: false, decision: null })
  const [isSubmittingDecision, setIsSubmittingDecision] = useState(false)

  const [previewImageUrl, setPreviewImageUrl] = useState('')
  const [previewImageFileName, setPreviewImageFileName] = useState('')
  const [showImagePreview, setShowImagePreview] = useState(false)
  const [viewLoadingByType, setViewLoadingByType] = useState({})

  const documentsByType = useMemo(() => {
    const map = {}

    documents.forEach((doc) => {
      const key = normalizeDocType(doc?.documentType)
      if (!map[key]) {
        map[key] = doc
      }
    })

    return map
  }, [documents])

  const filteredHistoryRows = useMemo(
    () => historyRows.filter((row) => String(row?.fromStatus ?? '') !== String(row?.toStatus ?? '')),
    [historyRows],
  )

  const currentStatus = normalizeStatus(application?.currentStatus || application?.status)
  const docActionsDisabled = ['UNDER_REVIEW', 'APPROVED', 'REJECTED'].includes(currentStatus)

  const showDocMessage = (docType, kind, message) => {
    setMessageByType((prev) => ({
      ...prev,
      [docType]: {
        ...(prev[docType] || {}),
        [kind]: message,
      },
    }))

    window.setTimeout(() => {
      setMessageByType((prev) => ({
        ...prev,
        [docType]: {
          ...(prev[docType] || {}),
          [kind]: '',
        },
      }))
    }, 3000)
  }

  const closeImagePreview = () => {
    if (previewImageUrl) {
      window.URL.revokeObjectURL(previewImageUrl)
    }
    setPreviewImageFileName('')
    setPreviewImageUrl('')
    setShowImagePreview(false)
  }

  const handleDownloadPreview = () => {
    if (!previewImageUrl) {
      return
    }

    const anchor = document.createElement('a')
    anchor.href = previewImageUrl
    anchor.download = previewImageFileName || 'document'
    anchor.click()
  }

  const fetchDocuments = async () => {
    try {
      const docsResponse = await axiosInstance.get(`/gateway/documents/application/${id}`)
      console.log('AdminReview documents response:', docsResponse?.data)

      const docsPayload = docsResponse?.data
      const docsRows = Array.isArray(docsPayload)
        ? docsPayload
        : Array.isArray(docsPayload?.items)
          ? docsPayload.items
          : Array.isArray(docsPayload?.data)
            ? docsPayload.data
            : []

      setDocumentsLoadError(false)
      setDocuments(docsRows)
    } catch (error) {
      console.error('AdminReview documents fetch failed:', error)
      setDocumentsLoadError(true)
      setDocuments([])
    }
  }

  const loadData = async () => {
    setIsLoading(true)
    setPageError('')

    try {
      const [appResponse, historyResponse] = await Promise.all([
        axiosInstance.get(`/gateway/admin/applications/${id}`),
        axiosInstance.get(`/gateway/admin/applications/${id}/history`),
      ])

      console.log('AdminReview application response:', appResponse?.data)
      console.log('AdminReview history response:', historyResponse?.data)

      const appPayload = appResponse?.data?.data || appResponse?.data || {}
      const historyPayload = historyResponse?.data

      const historyItems = Array.isArray(historyPayload)
        ? historyPayload
        : Array.isArray(historyPayload?.items)
          ? historyPayload.items
          : Array.isArray(historyPayload?.data)
            ? historyPayload.data
            : []

      let docsItems = []
      try {
        const docsResponse = await axiosInstance.get(`/gateway/documents/application/${id}`)
        console.log('AdminReview documents response:', docsResponse?.data)

        const docsPayload = docsResponse?.data
        docsItems = Array.isArray(docsPayload)
          ? docsPayload
          : Array.isArray(docsPayload?.items)
            ? docsPayload.items
            : Array.isArray(docsPayload?.data)
              ? docsPayload.data
              : []
        setDocumentsLoadError(false)
      } catch (docsError) {
        console.error('AdminReview documents fetch failed:', docsError)
        docsItems = []
        setDocumentsLoadError(true)
      }

      setApplication(appPayload)
      setHistoryRows(historyItems)
      setDocuments(docsItems)
      setSanctionAmount(String(appPayload?.loanAmount ?? ''))
      setInterestRate('10.5')
    } catch (error) {
      console.error('AdminReview application/history fetch failed:', error)
      setPageError('Unable to load review details at the moment.')
      setApplication(null)
      setHistoryRows([])
      setDocuments([])
      setDocumentsLoadError(false)
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [id])

  useEffect(() => {
    return () => {
      if (previewImageUrl) {
        window.URL.revokeObjectURL(previewImageUrl)
      }
    }
  }, [previewImageUrl])

  const handleViewDocument = async (documentId, fileName, docType) => {
    if (!documentId) {
      if (docType) {
        showDocMessage(docType, 'error', 'Document not found for preview.')
      }
      return
    }

    if (docType) {
      setViewLoadingByType((prev) => ({ ...prev, [docType]: true }))
    }

    try {
      const token = sessionStorage.getItem('token')
      const response = await fetch(`http://localhost:8002/gateway/documents/${documentId}/file`, {
        headers: {
          Authorization: `Bearer ${token}`,
        },
      })

      if (!response.ok) {
        throw new Error('Failed')
      }

      const blob = await response.blob()
      const contentType = response.headers.get('content-type') || ''
      const url = window.URL.createObjectURL(blob)

      if (contentType.includes('image')) {
        if (previewImageUrl) {
          window.URL.revokeObjectURL(previewImageUrl)
        }
        setPreviewImageUrl(url)
        setPreviewImageFileName(fileName || 'document')
        setShowImagePreview(true)
      } else {
        window.open(url, '_blank')
      }
    } catch (error) {
      if (docType) {
        showDocMessage(docType, 'error', 'Unable to open document. Please try again.')
      }
    } finally {
      if (docType) {
        setViewLoadingByType((prev) => ({ ...prev, [docType]: false }))
      }
    }
  }

  const verifyDocument = async (docType, document, status, remarks) => {
    const documentId = getDocId(document)
    if (!documentId) {
      showDocMessage(docType, 'error', 'Document id missing.')
      return
    }

    setVerifyLoadingByType((prev) => ({ ...prev, [docType]: true }))
    setDocumentActionError('')

    try {
      await axiosInstance.put(`/gateway/admin/documents/${documentId}/verify`, {
        status,
        remarks,
      })

      showDocMessage(docType, 'success', `Document ${status.toLowerCase()} successfully.`)
      await fetchDocuments()
    } catch (error) {
      setDocumentActionError('Unable to update document status. Please try again.')
    } finally {
      setVerifyLoadingByType((prev) => ({ ...prev, [docType]: false }))
    }
  }

  const handleApproveReject = (decision) => {
    setDecisionError('')

    if (!decisionRemarks.trim()) {
      setDecisionError('Remarks are required to submit decision.')
      return
    }

    setDecisionModal({ open: true, decision })
  }

  const handleMarkUnderReview = () => {
    setDecisionError('')
    setDecisionModal({ open: true, decision: 'UNDER_REVIEW' })
  }

  const submitDecision = async () => {
    if (!decisionModal.decision) {
      return
    }

    setIsSubmittingDecision(true)
    setDecisionError('')
    setDecisionSuccess('')

    try {
      const payload =
        decisionModal.decision === 'UNDER_REVIEW'
          ? {
              decision: 'UNDER_REVIEW',
              remarks: 'Application is under review',
            }
          : decisionModal.decision === 'APPROVED'
            ? {
                decision: 'APPROVED',
                remarks: decisionRemarks,
                sanctionAmount: parseFloat(sanctionAmount),
                interestRate: parseFloat(interestRate),
              }
            : {
                decision: 'REJECTED',
                remarks: decisionRemarks,
                sanctionAmount: 0,
                interestRate: 0,
              }

      await axiosInstance.post(`/gateway/admin/applications/${id}/decision`, payload)
      setDecisionSuccess('Decision submitted successfully')
      setDecisionModal({ open: false, decision: null })

      if (decisionModal.decision === 'UNDER_REVIEW') {
        await loadData()
      } else {
        window.setTimeout(() => {
          navigate('/admin/queue')
        }, 2000)
      }
    } catch (error) {
      setDecisionError('Unable to submit decision. Please try again.')
    } finally {
      setIsSubmittingDecision(false)
    }
  }

  return (
    <section className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Application Review</h1>
        <p className="mt-1 text-sm text-slate-600">Review submitted documents and make final decision.</p>
      </div>

      {isLoading ? <LoadingSpinner /> : null}

      {!isLoading && pageError ? (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm font-medium text-red-700">{pageError}</p>
      ) : null}

      {!isLoading && !pageError && application ? (
        <div className="grid gap-6 xl:grid-cols-[1.6fr_1fr]">
          <div className="space-y-6">
            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-3 text-lg font-semibold text-slate-900">Application Details</h2>
              <dl className="grid gap-3 text-sm text-slate-700 sm:grid-cols-2">
                <div><dt className="font-medium text-slate-500">Applicant Name</dt><dd>{application?.fullName || application?.applicantName || '-'}</dd></div>
                <div><dt className="font-medium text-slate-500">Email</dt><dd>{application?.email || application?.personalDetails?.email || '-'}</dd></div>
                <div><dt className="font-medium text-slate-500">Phone</dt><dd>{application?.phoneNumber || application?.phone || application?.personalDetails?.phone || '-'}</dd></div>
                <div><dt className="font-medium text-slate-500">Loan Amount</dt><dd>{formatCurrency(application?.loanAmount || application?.loanDetails?.requestedAmount)}</dd></div>
                <div><dt className="font-medium text-slate-500">Loan Purpose</dt><dd>{application?.loanPurpose || application?.loanDetails?.loanPurpose || '-'}</dd></div>
                <div><dt className="font-medium text-slate-500">Tenure</dt><dd>{application?.tenureMonths || application?.loanDetails?.requestedTenureMonths || '-'} months</dd></div>
                <div><dt className="font-medium text-slate-500">Current Status</dt><dd><StatusBadge status={application?.currentStatus || application?.status || '-'} /></dd></div>
                <div><dt className="font-medium text-slate-500">Document Verification Status</dt><dd><StatusBadge status={application?.documentVerificationStatus || 'Pending'} /></dd></div>
              </dl>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-3 text-lg font-semibold text-slate-900">Status History</h2>
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-slate-200">
                  <thead className="bg-slate-50">
                    <tr>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">From Status</th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">To Status</th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Changed By</th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Remarks</th>
                      <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-slate-600">Date</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {filteredHistoryRows.map((row, index) => (
                      <tr key={`${row?.changedAt || 'row'}-${index}`}>
                        <td className="px-3 py-2 text-sm text-slate-700">{formatStatusLabel(row?.fromStatus)}</td>
                        <td className="px-3 py-2 text-sm text-slate-700">{formatStatusLabel(row?.toStatus)}</td>
                        <td className="px-3 py-2 text-sm text-slate-700">{row?.changedBy || '-'}</td>
                        <td className="px-3 py-2 text-sm text-slate-700">{row?.remarks || '-'}</td>
                        <td className="px-3 py-2 text-sm text-slate-700">{formatDate(row?.changedAt)}</td>
                      </tr>
                    ))}
                    {filteredHistoryRows.length === 0 ? (
                      <tr>
                        <td colSpan={5} className="px-3 py-6 text-center text-sm text-slate-600">No history found.</td>
                      </tr>
                    ) : null}
                  </tbody>
                </table>
              </div>
            </section>

            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-4 text-lg font-semibold text-slate-900">Documents</h2>
              {documentsLoadError ? (
                <p className="mb-3 text-xs text-slate-500">Documents not accessible via current permissions</p>
              ) : null}
              {documentActionError ? (
                <div className="mb-4 flex items-start justify-between gap-3 rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">
                  <span>{documentActionError}</span>
                  <button
                    type="button"
                    onClick={() => setDocumentActionError('')}
                    className="text-xs font-semibold text-red-700"
                  >
                    Dismiss
                  </button>
                </div>
              ) : null}
              <div className="grid gap-4 sm:grid-cols-2">
                {DOC_TYPE_CONFIG.map((config) => {
                  const doc = documentsByType[config.apiValue]
                  const state = messageByType[config.apiValue] || {}
                  const isBusy = verifyLoadingByType[config.apiValue]
                  const isViewing = viewLoadingByType[config.apiValue]
                  const showReject = !docActionsDisabled && rejectOpenByType[config.apiValue]

                  return (
                    <article key={config.apiValue} className="rounded-xl border border-slate-200 p-4">
                      <div className="mb-2 flex items-center justify-between">
                        <h3 className="text-sm font-semibold text-slate-900">{config.label}</h3>
                        {doc ? <StatusBadge status={doc?.status || 'Pending'} /> : null}
                      </div>

                      <p className="text-xs text-slate-600">{doc ? `File: ${doc?.originalFileName || doc?.fileName || '-'}` : 'No file uploaded'}</p>
                      <p className="mt-1 text-xs text-slate-500">Uploaded: {formatDate(doc?.uploadedAt)}</p>

                      {state.success ? <p className="mt-2 rounded-md bg-green-50 px-2.5 py-1.5 text-xs font-medium text-green-700">{state.success}</p> : null}

                      <div className="mt-3 flex flex-wrap items-center gap-2">
                        <button
                          type="button"
                          disabled={!doc || isViewing}
                          onClick={() =>
                            handleViewDocument(
                              doc?.id,
                              doc?.originalFileName || doc?.fileName || 'document',
                              config.apiValue,
                            )
                          }
                          className="rounded-md border border-slate-300 px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {isViewing ? 'Viewing...' : 'View'}
                        </button>

                        <button
                          type="button"
                          disabled={!doc || isBusy || docActionsDisabled}
                          onClick={() => verifyDocument(config.apiValue, doc, 'Verified', 'doc verified')}
                          className="rounded-md border border-green-200 px-2.5 py-1.5 text-xs font-semibold text-green-700 transition hover:bg-green-50 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          Verify
                        </button>

                        <button
                          type="button"
                          disabled={!doc || isBusy || docActionsDisabled}
                          onClick={() =>
                            setRejectOpenByType((prev) => ({
                              ...prev,
                              [config.apiValue]: !prev[config.apiValue],
                            }))
                          }
                          className="rounded-md border border-red-200 px-2.5 py-1.5 text-xs font-semibold text-red-700 transition hover:bg-red-50 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          Reject
                        </button>
                      </div>

                      {showReject ? (
                        <div className="mt-3 space-y-2">
                          <input
                            type="text"
                            value={rejectRemarksByType[config.apiValue] || ''}
                            onChange={(event) =>
                              setRejectRemarksByType((prev) => ({
                                ...prev,
                                [config.apiValue]: event.target.value,
                              }))
                            }
                            placeholder="Enter rejection remarks"
                            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-xs outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
                          />
                          <div className="flex items-center gap-2">
                            <button
                              type="button"
                              disabled={!doc || isBusy || !String(rejectRemarksByType[config.apiValue] || '').trim()}
                              onClick={() =>
                                verifyDocument(
                                  config.apiValue,
                                  doc,
                                  'Rejected',
                                  String(rejectRemarksByType[config.apiValue] || '').trim(),
                                )
                              }
                              className="rounded-md bg-red-600 px-2.5 py-1.5 text-xs font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
                            >
                              Submit Rejection
                            </button>
                            <button
                              type="button"
                              onClick={() =>
                                setRejectOpenByType((prev) => ({
                                  ...prev,
                                  [config.apiValue]: false,
                                }))
                              }
                              className="rounded-md border border-slate-300 px-2.5 py-1.5 text-xs font-semibold text-slate-700 transition hover:bg-slate-50"
                            >
                              Cancel
                            </button>
                          </div>
                        </div>
                      ) : null}
                    </article>
                  )
                })}
              </div>
              {docActionsDisabled ? (
                <p className="mt-3 text-xs text-slate-500">
                  Document actions are not available once the application is under review.
                </p>
              ) : null}
            </section>
          </div>

          <div className="space-y-6">
            <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
              <h2 className="mb-3 text-lg font-semibold text-slate-900">Decision Panel</h2>

              {currentStatus === 'DOCS_VERIFIED' ? (
                <div className="space-y-3">
                  {decisionError ? (
                    <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{decisionError}</p>
                  ) : null}

                  {decisionSuccess ? (
                    <p className="rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">{decisionSuccess}</p>
                  ) : null}

                  <button
                    type="button"
                    disabled={isSubmittingDecision}
                    onClick={handleMarkUnderReview}
                    className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Mark as Under Review
                  </button>
                </div>
              ) : currentStatus === 'UNDER_REVIEW' ? (
                <div className="space-y-3">
                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Remarks</label>
                    <textarea
                      rows={4}
                      value={decisionRemarks}
                      onChange={(event) => setDecisionRemarks(event.target.value)}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Sanction Amount</label>
                    <input
                      type="number"
                      value={sanctionAmount}
                      onChange={(event) => setSanctionAmount(event.target.value)}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-sm font-medium text-slate-700">Interest Rate</label>
                    <input
                      type="number"
                      step="0.1"
                      value={interestRate}
                      onChange={(event) => setInterestRate(event.target.value)}
                      className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm outline-none focus:border-blue-600 focus:ring-1 focus:ring-blue-600"
                    />
                  </div>

                  {decisionError ? (
                    <p className="rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">{decisionError}</p>
                  ) : null}

                  {decisionSuccess ? (
                    <p className="rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">{decisionSuccess}</p>
                  ) : null}

                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      disabled={isSubmittingDecision}
                      onClick={() => handleApproveReject('APPROVED')}
                      className="rounded-lg bg-green-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-green-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Approve
                    </button>

                    <button
                      type="button"
                      disabled={isSubmittingDecision}
                      onClick={() => handleApproveReject('REJECTED')}
                      className="rounded-lg bg-red-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-red-700 disabled:cursor-not-allowed disabled:opacity-60"
                    >
                      Reject
                    </button>
                  </div>
                </div>
              ) : currentStatus === 'APPROVED' || currentStatus === 'REJECTED' ? (
                <div className="space-y-2">
                  <p className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-700">
                    A decision has already been made for this application.
                  </p>
                  <div>
                    <StatusBadge status={currentStatus} />
                  </div>
                </div>
              ) : (
                <p className="rounded-lg bg-slate-100 px-3 py-2 text-sm text-slate-600">
                  Decision can be made once documents are verified
                </p>
              )}
            </section>
          </div>
        </div>
      ) : null}

      {showImagePreview ? (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/80" onClick={closeImagePreview}>
          <div className="absolute right-4 top-4 flex items-center gap-2">
            <button
              type="button"
              onClick={handleDownloadPreview}
              className="rounded-full p-2 text-white hover:bg-white/20"
              aria-label="Download preview image"
            >
              <Download size={22} />
            </button>
            <button
              type="button"
              onClick={closeImagePreview}
              className="rounded-full p-2 text-white hover:bg-white/20"
              aria-label="Close image preview"
            >
              <X size={22} />
            </button>
          </div>

          <img
            src={previewImageUrl}
            alt="Document preview"
            onClick={(event) => event.stopPropagation()}
            className="max-h-screen max-w-3xl object-contain"
          />
        </div>
      ) : null}

      <Modal
        isOpen={decisionModal.open}
        onClose={() => setDecisionModal({ open: false, decision: null })}
        title="Confirm Decision"
      >
        <p className="mb-4">
          {decisionModal.decision === 'UNDER_REVIEW'
            ? 'Mark this application as Under Review?'
            : `Are you sure you want to ${decisionModal.decision === 'APPROVED' ? 'approve' : 'reject'} this application?`}
        </p>
        <div className="flex items-center justify-end gap-2">
          <button
            type="button"
            onClick={() => setDecisionModal({ open: false, decision: null })}
            className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-semibold text-slate-700"
          >
            Cancel
          </button>
          <button
            type="button"
            disabled={isSubmittingDecision}
            onClick={submitDecision}
            className="rounded-lg bg-blue-600 px-3 py-2 text-sm font-semibold text-white"
          >
            {isSubmittingDecision ? 'Submitting...' : 'Confirm'}
          </button>
        </div>
      </Modal>
    </section>
  )
}

export default AdminReviewPage
