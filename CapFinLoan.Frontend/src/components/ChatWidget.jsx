import { MessageCircle, Mic, MicOff, X } from 'lucide-react'
import { useEffect, useMemo, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import StatusBadge from './StatusBadge'
import useAuthStore from '../store/authStore'

const starterPrompts = [
  'What documents do I need?',
  'Explain the application process',
  'What does DocsPending mean?'
]

function ChatWidget() {
  const navigate = useNavigate()
  const { token } = useAuthStore()

  const [isOpen, setIsOpen] = useState(false)
  const [input, setInput] = useState('')
  const [messages, setMessages] = useState([])
  const [isLoading, setIsLoading] = useState(false)
  const [hasSeen, setHasSeen] = useState(false)
  const [isListening, setIsListening] = useState(false)
  const [voiceSupportMessage, setVoiceSupportMessage] = useState('')

  const scrollRef = useRef(null)
  const recognitionRef = useRef(null)

  const conversationHistory = useMemo(
    () =>
      messages.map((message) => ({
        role: message.role,
        content: message.content,
      })),
    [messages],
  )

  useEffect(() => {
    if (!isOpen) {
      return
    }

    const container = scrollRef.current
    if (container) {
      container.scrollTop = container.scrollHeight
    }
  }, [messages, isLoading, isOpen])

  const resetChat = () => {
    setMessages([])
    setInput('')
    setIsLoading(false)
    setIsListening(false)
    setVoiceSupportMessage('')
    if (recognitionRef.current) {
      recognitionRef.current.stop()
      recognitionRef.current = null
    }
  }

  const toggleChat = () => {
    if (isOpen) {
      setIsOpen(false)
      resetChat()
    } else {
      setIsOpen(true)
      if (!hasSeen) {
        setHasSeen(true)
      }
    }
  }

  const startListening = () => {
    const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition
    if (!SpeechRecognition) {
      setVoiceSupportMessage('Voice input not supported in this browser. Please use Chrome or Edge.')
      return
    }

    setVoiceSupportMessage('')
    const recognition = new SpeechRecognition()
    recognition.lang = 'en-US'
    recognition.continuous = true
    recognition.interimResults = true
    recognition.maxAlternatives = 1

    recognition.onresult = (event) => {
      let finalTranscript = ''
      let interimTranscript = ''

      for (let i = event.resultIndex; i < event.results.length; i += 1) {
        if (event.results[i].isFinal) {
          finalTranscript += event.results[i][0].transcript
        } else {
          interimTranscript += event.results[i][0].transcript
        }
      }

      setInput(finalTranscript || interimTranscript)
    }

    recognition.onerror = (error) => {
      setIsListening(false)
      recognitionRef.current = null
      console.error('Speech recognition error:', error)
    }

    recognitionRef.current = recognition
    setIsListening(true)
    recognition.start()
  }

  const stopListening = () => {
    if (recognitionRef.current) {
      recognitionRef.current.stop()
      recognitionRef.current = null
    }
    setIsListening(false)
  }

  const sendMessage = async (messageText) => {
    const trimmed = messageText.trim()
    if (!trimmed || isLoading) {
      return
    }

    const userMessage = { role: 'user', content: trimmed }
    setMessages((prev) => [...prev, userMessage])
    setInput('')
    setIsLoading(true)

    try {
      const response = await fetch('http://localhost:8002/gateway/chat/message', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          message: trimmed,
          conversationHistory,
          authToken: token,
        }),
      })

      if (!response.ok) {
        throw new Error(`Chat request failed: ${response.status}`)
      }

      const data = await response.json()
      const replyText = data?.reply || 'Sorry, I could not respond right now.'
      const applications = Array.isArray(data?.applications) ? data.applications : []

      setMessages((prev) => [...prev, { role: 'assistant', content: replyText, applications }])
    } catch (error) {
      setMessages((prev) => [
        ...prev,
        {
          role: 'assistant',
          content: "I'm currently unavailable. Please try again later.",
        },
      ])
    } finally {
      setIsLoading(false)
    }
  }

  const handleSubmit = (event) => {
    event.preventDefault()
    if (isListening) {
      return
    }
    sendMessage(input)
  }

  const formatCurrency = (amount) =>
    new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      maximumFractionDigits: 0,
    }).format(amount || 0)

  const resolveShortId = (application) => {
    if (application?.shortId) {
      return application.shortId
    }

    const rawId = String(application?.applicationId || '')
    return rawId.length >= 8 ? rawId.slice(0, 8) : rawId
  }

  return (
    <div className="fixed bottom-6 right-6 z-50">
      {isOpen ? (
        <div className="flex h-[70vh] max-h-[500px] w-[90vw] max-w-[380px] flex-col overflow-hidden rounded-2xl border border-slate-200 bg-white shadow-2xl">
          <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
            <span className="text-sm font-semibold text-slate-900">CapFinLoan Assistant</span>
            <button
              type="button"
              onClick={toggleChat}
              className="rounded-full p-1 text-slate-500 transition hover:bg-slate-100 hover:text-slate-700"
              aria-label="Close chat"
            >
              <X size={18} />
            </button>
          </div>

          <div ref={scrollRef} className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
            {messages.length === 0 ? (
              <div className="space-y-3">
                <p className="text-xs text-slate-500">Try asking:</p>
                <div className="flex flex-wrap gap-2">
                  {starterPrompts.map((prompt) => (
                    <button
                      key={prompt}
                      type="button"
                      onClick={() => sendMessage(prompt)}
                      className="rounded-full border border-slate-200 px-3 py-1 text-xs text-slate-600 transition hover:border-blue-200 hover:bg-blue-50 hover:text-blue-700"
                    >
                      {prompt}
                    </button>
                  ))}
                </div>
              </div>
            ) : null}

            {messages.map((message, index) => (
              <div
                key={`${message.role}-${index}`}
                className={`flex ${message.role === 'user' ? 'justify-end' : 'justify-start'}`}
              >
                <div
                  className={`max-w-[75%] rounded-2xl px-3 py-2 text-xs leading-relaxed sm:text-sm ${
                    message.role === 'user'
                      ? 'bg-blue-600 text-white'
                      : 'bg-slate-100 text-slate-800'
                  }`}
                >
                  <p>{message.content}</p>
                  {message.role === 'assistant' && message.applications?.length ? (
                    <div className="mt-3 flex flex-col gap-2">
                      {message.applications.map((application) => (
                        <button
                          key={application.applicationId}
                          type="button"
                          onClick={() => navigate(`/applicant/application/${application.applicationId}`)}
                          className="rounded-lg border border-gray-200 bg-white p-3 text-left text-xs transition hover:border-blue-300 hover:bg-blue-50"
                        >
                          <div className="flex items-center justify-between gap-2">
                            <span className="font-semibold text-slate-900">#{resolveShortId(application)}</span>
                            <StatusBadge status={application.status} />
                          </div>
                          <div className="mt-2 text-slate-600">
                            <div className="text-sm font-semibold text-slate-900">
                              {formatCurrency(application.loanAmount)}
                            </div>
                            <div className="text-xs text-slate-500">{application.loanPurpose || 'Loan'}</div>
                          </div>
                        </button>
                      ))}
                    </div>
                  ) : null}
                </div>
              </div>
            ))}

            {isLoading ? (
              <div className="flex justify-start">
                <div className="flex items-center gap-1 rounded-2xl bg-slate-100 px-3 py-2 text-xs text-slate-500">
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400 [animation-delay:-0.3s]" />
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400 [animation-delay:-0.15s]" />
                  <span className="h-2 w-2 animate-bounce rounded-full bg-slate-400" />
                </div>
              </div>
            ) : null}
          </div>

          <form onSubmit={handleSubmit} className="border-t border-slate-200 px-3 py-3">
            <div className="flex items-center gap-2">
              <input
                type="text"
                value={input}
                onChange={(event) => setInput(event.target.value)}
                placeholder="Ask about your application..."
                className="flex-1 rounded-full border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
              />
              <button
                type="button"
                onClick={isListening ? stopListening : startListening}
                className={`rounded-full p-2 transition ${
                  isListening
                    ? 'bg-red-100 text-red-500 animate-pulse'
                    : 'text-gray-500 hover:bg-gray-100'
                }`}
                aria-label={isListening ? 'Stop voice input' : 'Start voice input'}
              >
                {isListening ? <MicOff size={18} /> : <Mic size={18} />}
              </button>
              <button
                type="submit"
                disabled={!input.trim() || isLoading || isListening}
                className="rounded-full bg-blue-600 px-4 py-2 text-xs font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                Send
              </button>
            </div>
            {isListening ? (
              <p className="mt-2 text-xs text-red-500">Listening... speak now</p>
            ) : null}
            {voiceSupportMessage ? (
              <p className="mt-2 text-xs text-slate-500">{voiceSupportMessage}</p>
            ) : null}
          </form>
        </div>
      ) : (
        <div className="relative">
          {!hasSeen ? (
            <div className="absolute inset-0 rounded-full bg-blue-400 opacity-30 animate-ping" />
          ) : null}
          <button
            type="button"
            onClick={toggleChat}
            className="relative flex h-16 w-16 items-center justify-center rounded-full bg-blue-600 text-white shadow-lg transition hover:bg-blue-700"
            aria-label="Open chat"
          >
            <MessageCircle size={28} />
          </button>
        </div>
      )}
    </div>
  )
}

export default ChatWidget
