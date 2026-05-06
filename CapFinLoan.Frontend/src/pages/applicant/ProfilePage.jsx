import { useEffect, useState } from 'react'
import axiosInstance from '../../api/axiosInstance'
import PageTitle from '../../components/PageTitle'

function ProfilePage() {
  const [profile, setProfile] = useState(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isEditing, setIsEditing] = useState(false)
  const [formValues, setFormValues] = useState({ name: '', phoneNumber: '' })
  const [profileMessage, setProfileMessage] = useState('')
  const [profileError, setProfileError] = useState('')
  const [passwordValues, setPasswordValues] = useState({
    currentPassword: '',
    newPassword: '',
    confirmPassword: '',
  })
  const [passwordMessage, setPasswordMessage] = useState('')
  const [passwordError, setPasswordError] = useState('')

  useEffect(() => {
    let isMounted = true

    const loadProfile = async () => {
      setIsLoading(true)
      setProfileError('')

      try {
        const response = await axiosInstance.get('/gateway/auth/profile')
        const payload = response?.data || null

        if (isMounted) {
          setProfile(payload)
          setFormValues({
            name: payload?.name || '',
            phoneNumber: payload?.phoneNumber || '',
          })
        }
      } catch (error) {
        if (isMounted) {
          setProfileError('Unable to load your profile right now.')
        }
      } finally {
        if (isMounted) {
          setIsLoading(false)
        }
      }
    }

    loadProfile()

    return () => {
      isMounted = false
    }
  }, [])

  const handleEditToggle = () => {
    setProfileMessage('')
    setProfileError('')

    if (isEditing && profile) {
      setFormValues({
        name: profile.name || '',
        phoneNumber: profile.phoneNumber || '',
      })
    }

    setIsEditing((prev) => !prev)
  }

  const handleProfileChange = (event) => {
    const { name, value } = event.target
    setFormValues((prev) => ({ ...prev, [name]: value }))
  }

  const handlePasswordChange = (event) => {
    const { name, value } = event.target
    setPasswordValues((prev) => ({ ...prev, [name]: value }))
  }

  const handleProfileSave = async () => {
    setProfileMessage('')
    setProfileError('')

    if (!formValues.name.trim()) {
      setProfileError('Name is required.')
      return
    }

    try {
      const response = await axiosInstance.put('/gateway/auth/profile', {
        name: formValues.name.trim(),
        phoneNumber: formValues.phoneNumber?.trim() || '',
      })

      if (response?.data?.success) {
        setProfile((prev) =>
          prev
            ? {
                ...prev,
                name: formValues.name.trim(),
                phoneNumber: formValues.phoneNumber?.trim() || '',
              }
            : prev,
        )
        setProfileMessage('Profile updated successfully')
        setIsEditing(false)
      } else {
        setProfileError(response?.data?.message || 'Unable to update profile.')
      }
    } catch (error) {
      setProfileError('Unable to update profile. Please try again.')
    }
  }

  const handlePasswordSave = async () => {
    setPasswordMessage('')
    setPasswordError('')

    if (!passwordValues.currentPassword || !passwordValues.newPassword) {
      setPasswordError('Please fill in all password fields.')
      return
    }

    if (passwordValues.newPassword.length < 8) {
      setPasswordError('New password must be at least 8 characters.')
      return
    }

    if (passwordValues.newPassword !== passwordValues.confirmPassword) {
      setPasswordError('New passwords do not match.')
      return
    }

    try {
      const response = await axiosInstance.put('/gateway/auth/profile/password', {
        currentPassword: passwordValues.currentPassword,
        newPassword: passwordValues.newPassword,
      })

      if (response?.data?.success) {
        setPasswordMessage('Password updated successfully')
        setPasswordValues({ currentPassword: '', newPassword: '', confirmPassword: '' })
      } else {
        setPasswordError(response?.data?.message || 'Unable to update password.')
      }
    } catch (error) {
      setPasswordError('Unable to update password. Please try again.')
    }
  }

  return (
    <div className="max-w-4xl">
      <PageTitle title="My Profile" subtitle="Manage your personal details and security settings." />

      {isLoading ? <p className="text-sm text-slate-500">Loading profile...</p> : null}
      {profileError ? (
        <p className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
          {profileError}
        </p>
      ) : null}

      {profile ? (
        <div className="space-y-6">
          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <div className="flex flex-wrap items-center justify-between gap-4">
              <h2 className="text-lg font-semibold text-slate-900">Personal Information</h2>
              <button
                type="button"
                onClick={handleEditToggle}
                className="rounded-full border border-slate-200 px-4 py-1.5 text-xs font-semibold text-slate-700 transition hover:border-blue-200 hover:text-blue-700"
              >
                {isEditing ? 'Cancel' : 'Edit'}
              </button>
            </div>

            <div className="mt-4 grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-xs font-semibold uppercase text-slate-500">Name</p>
                {isEditing ? (
                  <input
                    type="text"
                    name="name"
                    value={formValues.name}
                    onChange={handleProfileChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
                  />
                ) : (
                  <p className="mt-2 text-sm text-slate-800">{profile.name}</p>
                )}
              </div>

              <div>
                <p className="text-xs font-semibold uppercase text-slate-500">Email</p>
                <p className="mt-2 text-sm text-slate-800">{profile.email}</p>
              </div>

              <div>
                <p className="text-xs font-semibold uppercase text-slate-500">Phone Number</p>
                {isEditing ? (
                  <input
                    type="text"
                    name="phoneNumber"
                    value={formValues.phoneNumber}
                    onChange={handleProfileChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
                  />
                ) : (
                  <p className="mt-2 text-sm text-slate-800">{profile.phoneNumber || '-'}</p>
                )}
              </div>
            </div>

            {profileMessage ? (
              <p className="mt-4 rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">
                {profileMessage}
              </p>
            ) : null}
            {profileError && !isLoading ? (
              <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                {profileError}
              </p>
            ) : null}

            {isEditing ? (
              <div className="mt-4 flex flex-wrap gap-3">
                <button
                  type="button"
                  onClick={handleProfileSave}
                  className="rounded-full bg-blue-600 px-4 py-2 text-xs font-semibold text-white transition hover:bg-blue-700"
                >
                  Save
                </button>
                <button
                  type="button"
                  onClick={handleEditToggle}
                  className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold text-slate-700 transition hover:border-blue-200 hover:text-blue-700"
                >
                  Cancel
                </button>
              </div>
            ) : null}
          </div>

          <div className="rounded-2xl border border-slate-200 bg-white p-6 shadow-sm">
            <h2 className="text-lg font-semibold text-slate-900">Security</h2>

            {profile.authProvider === 'GOOGLE' ? (
              <p className="mt-4 text-sm text-slate-600">
                You signed in with Google. Password management is handled by Google.
              </p>
            ) : (
              <div className="mt-4 grid gap-4 sm:grid-cols-2">
                <div>
                  <label className="text-xs font-semibold uppercase text-slate-500" htmlFor="currentPassword">
                    Current Password
                  </label>
                  <input
                    id="currentPassword"
                    name="currentPassword"
                    type="password"
                    value={passwordValues.currentPassword}
                    onChange={handlePasswordChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase text-slate-500" htmlFor="newPassword">
                    New Password
                  </label>
                  <input
                    id="newPassword"
                    name="newPassword"
                    type="password"
                    value={passwordValues.newPassword}
                    onChange={handlePasswordChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
                  />
                </div>

                <div>
                  <label className="text-xs font-semibold uppercase text-slate-500" htmlFor="confirmPassword">
                    Confirm New Password
                  </label>
                  <input
                    id="confirmPassword"
                    name="confirmPassword"
                    type="password"
                    value={passwordValues.confirmPassword}
                    onChange={handlePasswordChange}
                    className="mt-2 w-full rounded-xl border border-slate-200 px-3 py-2 text-sm outline-none focus:border-blue-500"
                  />
                </div>

                <div className="flex items-end">
                  <button
                    type="button"
                    onClick={handlePasswordSave}
                    className="rounded-full bg-blue-600 px-4 py-2 text-xs font-semibold text-white transition hover:bg-blue-700"
                  >
                    Save Password
                  </button>
                </div>
              </div>
            )}

            {passwordMessage ? (
              <p className="mt-4 rounded-lg bg-green-50 px-3 py-2 text-sm font-medium text-green-700">
                {passwordMessage}
              </p>
            ) : null}
            {passwordError ? (
              <p className="mt-4 rounded-lg bg-red-50 px-3 py-2 text-sm font-medium text-red-700">
                {passwordError}
              </p>
            ) : null}
          </div>
        </div>
      ) : null}
    </div>
  )
}

export default ProfilePage
