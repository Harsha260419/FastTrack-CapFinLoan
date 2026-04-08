import { Route, Routes } from 'react-router-dom'
import AdminLayout from './layouts/AdminLayout'
import ApplicantLayout from './layouts/ApplicantLayout'
import MainLayout from './layouts/MainLayout'
import ApplicantApplyPage from './pages/ApplicantApplyPage'
import ApplicantDocumentsPage from './pages/ApplicantDocumentsPage'
import ApplicantStatusPage from './pages/ApplicantStatusPage'
import AdminDashboardPage from './pages/admin/AdminDashboardPage'
import AdminQueuePage from './pages/admin/AdminQueuePage'
import AdminReportsPage from './pages/AdminReportsPage'
import AdminReviewPage from './pages/admin/AdminReviewPage'
import ApplicationDetailPage from './pages/applicant/ApplicationDetailPage'
import ApplicantDashboardPage from './pages/applicant/DashboardPage'
import LandingPage from './pages/LandingPage'
import LoginPage from './pages/LoginPage'
import SignupPage from './pages/SignupPage'
import { RequireAdmin, RequireAuth } from './utils/roleRoutes'

function App() {
  return (
    <Routes>
      <Route element={<MainLayout />}>
        <Route path="/" element={<LandingPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/signup" element={<SignupPage />} />

        <Route element={<RequireAuth />}>
          <Route path="/applicant" element={<ApplicantLayout />}>
            <Route path="dashboard" element={<ApplicantDashboardPage />} />
            <Route path="apply" element={<ApplicantApplyPage />} />
            <Route path="documents" element={<ApplicantDocumentsPage />} />
            <Route path="application/:id" element={<ApplicationDetailPage />} />
            <Route path="status" element={<ApplicantStatusPage />} />
            <Route path="status/:id" element={<ApplicantStatusPage />} />
          </Route>

          <Route element={<RequireAdmin />}>
            <Route path="/admin" element={<AdminLayout />}>
              <Route path="dashboard" element={<AdminDashboardPage />} />
              <Route path="queue" element={<AdminQueuePage />} />
              <Route path="review/:id" element={<AdminReviewPage />} />
              <Route path="reports" element={<AdminReportsPage />} />
            </Route>
          </Route>
        </Route>
      </Route>
    </Routes>
  )
}

export default App
