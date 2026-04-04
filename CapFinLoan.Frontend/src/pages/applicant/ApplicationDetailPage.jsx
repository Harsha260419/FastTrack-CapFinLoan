import { Check, Upload } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import axiosInstance from '../../api/axiosInstance'
import LoadingSpinner from '../../components/LoadingSpinner'
import StatusBadge from '../../components/StatusBadge'

const BASE_STATUS_ORDER = ['Draft', 'Submitted', 'DocsPending', 'DocsVerified', 'UnderReview', 'Closed']
const DOC_TYPE_CONFIG = [
  { label: 'ID Proof', apiValue: 'ID_PROOF' },
  { label: 'Address Proof', apiValue: 'ADDRESS_PROOF' },
  { label: 'Bank Statement', apiValue: 'BANK_STATEMENT' },
  { label: 'Income Proof', apiValue: 'INCOME_PROOF' },
]

function formatCurrency(value) {
  const amount = Number(value)
  if (Number.isNaN(amount)) {
    return '-'
  }

  return amount.toLocaleString('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 })
}

function formatDate(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleString()
}

function formatTimelineDate(value) {
  if (!value) {
    return '-'
  }

  const parsed = new Date(value)
  if (Number.isNaN(parsed.getTime())) {
    return '-'
  }

  return parsed.toLocaleDateString()
}

function toReadableStatus(status) {
  const normalized = String(status || '').trim()
  const map = {
    Draft: 'Draft',
    Submitted: 'Submitted',
    DocsPending: 'Docs Pending',
    DocsVerified: 'Docs Verified',
    UnderReview: 'Under Review',
    Approved: 'Approved',
    Rejected: 'Rejected',
    Closed: 'Closed',
    Pending: 'Pending',
    Verified: 'Verified',
  }

  return map[normalized] || normalized || '-'
}

function toReadableDocumentType(documentType) {
  const value = String(documentType || '').trim()
  const known = {
    IdProof: 'Id Proof',
    AddressProof: 'Address Proof',
    BankStatement: 'Bank Statement',
    IncomeProof: 'Income Proof',
    ID_PROOF: 'Id Proof',
    ADDRESS_PROOF: 'Address Proof',
    BANK_STATEMENT: 'Bank Statement',
    INCOME_PROOF: 'Income Proof',
  }

  if (known[value]) {
    return known[value]
  }

  return value
    .replace(/_/g, ' ')
    .replace(/([a-z0-9])([A-Z])/g, '$1 $2')
    .trim()
}

function normalizeDocumentTypeForApi(documentType) {
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

function getDocumentId(document) {
  return document?.id || document?.documentId || null
}

function getStatusOrder(currentStatus) {
  const decision = currentStatus === 'Rejected' ? 'Rejected' : 'Approved'
  return [...BASE_STATUS_ORDER.slice(0, 5), decision, 'Closed']
}

function ApplicationDetailPage() {
  const { id } = useParams()
  const fileInputRefs = useRef({})

  const [application, setApplication] = useState(null)
  const [timeline, setTimeline] = useState([])
  const [documents, setDocuments] = useState([])
  const [isLoading, setIsLoading] = useState(true)
  const [pageError, setPageError] = useState('')
  const [selectedFilesByType, setSelectedFilesByType] = useState({})
  const [uploadStatesByType, setUploadStatesByType] = useState({})

  const currentStatus = String(application?.status || '')
  const statusOrder = useMemo(() => getStatusOrder(currentStatus), [currentStatus])

  const canUploadDocuments = currentStatus === 'Submitted' || currentStatus === 'DocsPending'

  const applicationSummary = useMemo(() => {
    const firstName = application?.personalDetails?.firstName || ''
    const lastName = application?.personalDetails?.lastName || ''
    const fullName = `${firstName} ${lastName}`.trim()

    return {
      applicationId: application?.applicationId || '-',
      fullName: fullName || '-',
      loanAmount: application?.loanDetails?.requestedAmount,
      loanPurpose: application?.loanDetails?.loanPurpose || '-',
      tenureMonths: application?.loanDetails?.requestedTenureMonths ?? '-',
      status: application?.status,
    }
  }, [application])

  const documentsByType = useMemo(() => {
    const map = {}

    documents.forEach((document) => {
      const normalized = normalizeDocumentTypeForApi(document?.documentType)
      if (!map[normalized]) {
        map[normalized] = document
      }
    })

    return map
  }, [documents])

  const fetchDocuments = async () => {
    const documentsResponse = await axiosInstance.get(`/gateway/documents/application/${id}`)
    const documentsPayload = documentsResponse?.data
    const documentRows = Array.isArray(documentsPayload)
      ? documentsPayload
      : Array.isArray(documentsPayload?.items)
        ? documentsPayload.items
        : Array.isArray(documentsPayload?.data)
          ? documentsPayload.data
          : []
    setDocuments(documentRows)
  }

  const loadData = async () => {
    setIsLoading(true)
    setPageError('')

    try {
      const [applicationResponse, timelineResponse, documentsResponse] = await Promise.all([
        axiosInstance.get(`/gateway/applications/${id}`),
        axiosInstance.get(`/gateway/applications/${id}/status`),
        axiosInstance.get(`/gateway/documents/application/${id}`),
      ])

      console.log('Application detail response[0].data:', applicationResponse?.data)

      const appEnvelope = applicationResponse?.data || {}
      const appPayload = appEnvelope?.applicationId ? appEnvelope : appEnvelope?.data || {}
      const timelinePayload = timelineResponse?.data
      const documentsPayload = documentsResponse?.data

      const timelineEnvelope = timelinePayload || {}
      const timelineSource = Array.isArray(timelineEnvelope)
        ? timelineEnvelope
        : Array.isArray(timelineEnvelope?.timeline)
          ? timelineEnvelope.timeline
          : Array.isArray(timelineEnvelope?.data?.timeline)
            ? timelineEnvelope.data.timeline
            : Array.isArray(timelineEnvelope?.items)
              ? timelineEnvelope.items
              : Array.isArray(timelineEnvelope?.data)
                ? timelineEnvelope.data
                : []

      const timelineRows = timelineSource.map((entry) => ({
        status: entry?.status,
        transitionDate: entry?.transitionDate,
        remarks: entry?.remarks,
        nextAction: entry?.nextAction,
      }))

      const documentRows = Array.isArray(documentsPayload)
        ? documentsPayload
        : Array.isArray(documentsPayload?.items)
          ? documentsPayload.items
          : Array.isArray(documentsPayload?.data)
            ? documentsPayload.data
            : []

      setApplication(appPayload)
      setTimeline(timelineRows)
      setDocuments(documentRows)
    } catch (error) {
      setPageError('Unable to load application details at the moment.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    loadData()
  }, [id])

  const timelineMap = useMemo(() => {
    const map = new Map()

    timeline.forEach((entry) => {
      const normalized = String(entry?.status || '').trim()
      if (normalized) {
        map.set(normalized, entry)
      }
    })

    if (currentStatus && !map.has(currentStatus)) {
      map.set(currentStatus, {
        status: currentStatus,
        transitionDate: application?.updatedAt || application?.createdAt,
      })
    }

    return map
  }, [timeline, currentStatus, application])

  const currentIndex = statusOrder.indexOf(currentStatus)

  const setUploadState = (docType, statePatch) => {
    setUploadStatesByType((prev) => ({
      ...prev,
      [docType]: {
        ...(prev[docType] || {}),
        ...statePatch,
      },
    }))
  }

  const handleSelectFile = (docType, file) => {
    setUploadState(docType, { error: '', success: '' })

    if (!file) {
      setSelectedFilesByType((prev) => ({ ...prev, [docType]: null }))
      return
    }

    const allowedTypes = ['application/pdf', 'image/jpeg', 'image/png']
    if (!allowedTypes.includes(file.type)) {
      setUploadState(docType, { error: 'Only PDF, JPG, and PNG files are allowed.' })
      return
    }

    if (file.size > 5 * 1024 * 1024) {
      setUploadState(docType, { error: 'File exceeds 5MB limit.' })
      return
    }

    setSelectedFilesByType((prev) => ({ ...prev, [docType]: file }))
  }

  const handleDrop = (event, docType) => {
    event.preventDefault()
    const file = event.dataTransfer.files?.[0]
    handleSelectFile(docType, file)
  }

  const handleFileInputChange = (event, docType) => {
    const file = event.target.files?.[0]
    handleSelectFile(docType, file)
  }

  const openPicker = (docType) => {
    fileInputRefs.current[docType]?.click()
  }

  const handleUploadForType = async (docType) => {
    const selectedFile = selectedFilesByType[docType]
    const existingDocument = documentsByType[docType]
    const replaceDocumentId = getDocumentId(existingDocument)

    if (!selectedFile) {
      setUploadState(docType, { error: 'Please select a file to upload.' })
      return
    }

    if (!id) {
      setUploadState(docType, { error: 'Missing application identifier.' })
      return
    }

    setUploadState(docType, { isUploading: true, error: '', success: '' })

    try {
      const formData = new FormData()
      formData.append('ApplicationId', id)
      formData.append('DocumentType', docType)
      formData.append('File', selectedFile)

      if (replaceDocumentId) {
        await axiosInstance.put(`/gateway/documents/${replaceDocumentId}`, formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
      } else {
        await axiosInstance.post('/gateway/documents/upload', formData, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
      }

      setUploadState(docType, { success: 'Document uploaded successfully' })
      setSelectedFilesByType((prev) => ({ ...prev, [docType]: null }))
      if (fileInputRefs.current[docType]) {
        fileInputRefs.current[docType].value = ''
      }

      await fetchDocuments()
    } catch (error) {
      setUploadState(docType, { error: 'Unable to upload document. Please try again.' })
    } finally {
      setUploadState(docType, { isUploading: false })
    }
  }

  return (
    <section className="space-y-6">
      <Link to="/applicant/dashboard" className="inline-flex items-center text-sm font-semibold text-blue-700 hover:text-blue-800">
        ← Back to Dashboard
      </Link>

      {isLoading ? <LoadingSpinner /> : null}

      {!isLoading && pageError ? (
        <p className="rounded-lg bg-red-50 px-4 py-3 text-sm font-medium text-red-700">{pageError}</p>
      ) : null}

      {!isLoading && !pageError && application ? (
        <>
          <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h1 className="mb-4 text-xl font-bold text-slate-900">Application Summary</h1>
            <dl className="grid gap-4 text-sm text-slate-700 sm:grid-cols-2 lg:grid-cols-3">
              <div>
                <dt className="font-medium text-slate-500">Application ID</dt>
                <dd className="mt-1 font-semibold text-slate-900">{String(applicationSummary.applicationId || '-').slice(0, 8)}</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Full Name</dt>
                <dd className="mt-1">{applicationSummary.fullName}</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Loan Amount</dt>
                <dd className="mt-1">{formatCurrency(applicationSummary.loanAmount)}</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Loan Purpose</dt>
                <dd className="mt-1">{applicationSummary.loanPurpose}</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Tenure</dt>
                <dd className="mt-1">{applicationSummary.tenureMonths} months</dd>
              </div>
              <div>
                <dt className="font-medium text-slate-500">Current Status</dt>
                <dd className="mt-1">
                  <StatusBadge status={toReadableStatus(applicationSummary.status)} />
                </dd>
              </div>
            </dl>
          </section>

          <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h2 className="mb-4 text-lg font-semibold text-slate-900">Status Timeline</h2>

            <div className="space-y-0">
              {statusOrder.map((status, index) => {
                const entry = timelineMap.get(status)
                const isCurrent = status === currentStatus
                const isCompleted = currentIndex >= 0 ? index < currentIndex : Boolean(entry)
                const isFuture = !isCurrent && !isCompleted

                return (
                  <div key={status} className="flex gap-3 pb-4">
                    <div className="flex flex-col items-center">
                      <span
                        className={`inline-flex h-5 w-5 items-center justify-center rounded-full border ${
                          isCurrent
                            ? 'animate-pulse border-blue-600 bg-blue-600 text-white'
                            : isCompleted
                              ? 'border-blue-600 bg-blue-600 text-white'
                              : 'border-slate-300 bg-white text-transparent'
                        }`}
                      >
                        {isCompleted ? <Check size={12} /> : '•'}
                      </span>
                      {index < statusOrder.length - 1 ? <span className="mt-1 h-full w-px bg-slate-200" /> : null}
                    </div>

                    <div className="pb-1">
                      <p className="text-sm font-semibold text-slate-900">{toReadableStatus(status)}</p>
                      <p className="text-xs text-slate-600">{entry ? formatTimelineDate(entry?.transitionDate) : '-'}</p>
                      <p className="text-xs text-slate-600">{entry?.remarks || '-'}</p>
                      <p className="text-xs text-slate-500">{entry?.nextAction || '-'}</p>
                    </div>
                  </div>
                )
              })}
            </div>

            {currentStatus === 'Approved' ? (
              <p className="mt-4 rounded-lg bg-green-50 px-4 py-3 text-sm font-medium text-green-700">
                🎉 Congratulations! Your loan has been approved.
              </p>
            ) : null}

            {currentStatus === 'Rejected' ? (
              <p className="mt-4 rounded-lg bg-red-50 px-4 py-3 text-sm font-medium text-red-700">
                Your application was rejected.
              </p>
            ) : null}
          </section>

          <section className="rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
            <h2 className="mb-4 text-lg font-semibold text-slate-900">My Documents</h2>
            {canUploadDocuments ? (
              <div className="grid gap-4 sm:grid-cols-2">
                {DOC_TYPE_CONFIG.map((config) => {
                  const existingDocument = documentsByType[config.apiValue]
                  const fileName = existingDocument?.originalFileName || existingDocument?.fileName || '-'
                  const statusMap = {
                    Verified: 'Verified',
                    Pending: 'Pending',
                    Rejected: 'Rejected',
                  }
                  const statusValue = statusMap[existingDocument?.status] || toReadableStatus(existingDocument?.status)
                  const selectedFile = selectedFilesByType[config.apiValue]
                  const state = uploadStatesByType[config.apiValue] || {}

                  return (
                    <div key={config.apiValue} className="rounded-xl border border-slate-200 p-4">
                      <div className="mb-3 flex items-center justify-between gap-2">
                        <p className="text-sm font-semibold text-slate-900">{config.label}</p>
                        {existingDocument ? <StatusBadge status={statusValue} /> : null}
                      </div>

                      <p className="text-xs text-slate-600">{existingDocument ? `File: ${fileName}` : 'No file uploaded yet.'}</p>

                      <div
                        onDrop={(event) => handleDrop(event, config.apiValue)}
                        onDragOver={(event) => event.preventDefault()}
                        onClick={() => openPicker(config.apiValue)}
                        className="mt-3 cursor-pointer rounded-lg border-2 border-dashed border-blue-200 bg-blue-50/40 p-4 text-center"
                      >
                        <Upload className="mx-auto mb-2 text-blue-600" size={18} />
                        <p className="text-xs font-medium text-blue-700">Drag & drop or click to browse</p>
                        <p className="mt-1 text-[11px] text-slate-500">Accept .pdf, .jpg, .png (max 5MB)</p>
                      </div>

                      <input
                        ref={(element) => {
                          fileInputRefs.current[config.apiValue] = element
                        }}
                        type="file"
                        onChange={(event) => handleFileInputChange(event, config.apiValue)}
                        accept=".pdf,.jpg,.jpeg,.png"
                        className="hidden"
                      />

                      {selectedFile ? <p className="mt-2 text-xs text-slate-700">Selected: {selectedFile.name}</p> : null}

                      {state.error ? (
                        <p className="mt-2 rounded-md bg-red-50 px-2.5 py-1.5 text-xs font-medium text-red-700">{state.error}</p>
                      ) : null}

                      {state.success ? (
                        <p className="mt-2 rounded-md bg-green-50 px-2.5 py-1.5 text-xs font-medium text-green-700">{state.success}</p>
                      ) : null}

                      <div className="mt-3 flex items-center gap-2">
                        <button
                          type="button"
                          onClick={() => openPicker(config.apiValue)}
                          className="rounded-md border border-blue-200 px-2.5 py-1.5 text-xs font-semibold text-blue-700 transition hover:bg-blue-50"
                        >
                          {existingDocument ? 'Replace' : 'Upload'}
                        </button>

                        <button
                          type="button"
                          disabled={!selectedFile || state.isUploading}
                          onClick={() => handleUploadForType(config.apiValue)}
                          className="rounded-md bg-blue-600 px-2.5 py-1.5 text-xs font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
                        >
                          {state.isUploading ? 'Uploading...' : 'Confirm'}
                        </button>
                      </div>
                    </div>
                  )
                })}
              </div>
            ) : (
              <p className="text-sm text-slate-600">Documents can be uploaded only after submission or when documents are pending.</p>
            )}
          </section>
        </>
      ) : null}
    </section>
  )
}

export default ApplicationDetailPage
