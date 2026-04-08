# Frontend Context History (Persistent Log)

Last Updated: 2026-04-05
Workspace: FastTrack-CapFinLoan/CapFinLoan.Frontend

## Purpose
This document is the persistent context backup for frontend work so progress is not lost when chat context becomes limited.

How to use:
- Read this file before new frontend work.
- Append new changes after each major feature/fix.
- Keep endpoints, file paths, and behavior notes accurate.

## Frontend Stack and Core Setup
- React 18 + Vite (project name set to `cap-finloan`).
- Tailwind CSS v3 configured and used across app.
- React Router v6 with nested layout routes.
- Axios instance with auth interceptor and 401 redirect handling.
- Zustand auth store with sessionStorage persistence.
- React Hook Form for forms.
- Lucide icons for UI elements.

## Routing and Architecture
- Shared route shell and guards implemented in app entry routes.
- Applicant routes nested under `/applicant/*`.
- Admin routes nested under `/admin/*`.
- Auth guard utilities:
  - Require authenticated user.
  - Require admin role for admin pages.
- Later updates added review route:
  - `/admin/review/:id`

## Key Core Files Created/Updated
- `src/api/axiosInstance.js`
  - Base URL: `http://localhost:8002`
  - Bearer token injection from storage.
  - 401 handling redirects to `/login`.
- `src/store/authStore.js`
  - Refactored to store full auth response.
  - Hydrates `token`, `email`, `role` from sessionStorage.
  - Persists/removes those fields on login/logout.
- `src/utils/roleRoutes.jsx`
  - `RequireAuth` and `RequireAdmin` guard wrappers.
- `src/main.jsx`, `src/App.jsx`
  - BrowserRouter and nested route wiring.

## Shared UI Components and Layouts
Created and wired reusable UI:
- `src/components/Navbar.jsx`
- `src/components/Sidebar.jsx`
- `src/components/PageTitle.jsx`
- `src/components/PageCard.jsx`
- `src/components/StatusBadge.jsx`
- `src/components/LoadingSpinner.jsx`
- `src/components/Modal.jsx`

Layouts:
- `src/layouts/MainLayout.jsx`
- `src/layouts/ApplicantLayout.jsx`
- `src/layouts/AdminLayout.jsx`

Notes:
- Layout/root sizing was corrected to ensure full viewport usage.
- Admin sidebar users link was removed in latest admin update.

## Auth Features Implemented
### Login
- `src/pages/LoginPage.jsx`
- Submits to `/gateway/auth/login`.
- Stores full auth payload in Zustand.
- Redirects by role after login.

### Signup (Rewritten to OTP Flow)
- `src/pages/SignupPage.jsx`
- Step 1: send OTP to email via `/gateway/auth/signup/send-otp`.
- Includes resend timer logic.
- Step 2: complete signup using OTP code.
- Sends role as `APPLICANT` during signup.

## Applicant Module
### Applicant Dashboard
- `src/pages/applicant/DashboardPage.jsx`
- Uses paginated `items` response shape.
- Uses `applicationId`.
- Updated columns/actions:
  - View Details
  - Edit (draft only)
- Includes stale-cache timestamp query in requests.

### Applicant Apply Wizard (4 steps)
- `src/pages/ApplicantApplyPage.jsx`
- Supports:
  - Create draft
  - Update draft
  - Submit final application
- Supports edit mode via query string `?id=...`.
- Restricts edit behavior to draft workflows.
- Numeric conversions fixed (`parseFloat` / `parseInt`).
- Tenure options expanded to 12..180 months.

### Application Detail + Documents
- `src/pages/applicant/ApplicationDetailPage.jsx`
- Loads application summary, status/timeline, and documents.
- Fixed document boxes and upload/replace flows.
- View document behavior:
  - Uses native fetch with bearer token for file endpoint.
  - Images: in-app preview modal.
  - PDFs: opens in new tab.
- Added auto-dismiss success/error messages.
- Added image modal controls:
  - Download button
  - Close (X)

## Admin Module (Latest Major Work)
### New Admin Pages Added
- `src/pages/admin/AdminDashboardPage.jsx`
  - KPI cards from `/gateway/admin/dashboard`.
  - Recent applications from `/gateway/admin/applications`.
  - Review action links to `/admin/review/{applicationId}`.

- `src/pages/admin/AdminQueuePage.jsx`
  - Applications queue from `/gateway/admin/applications`.
  - Client-side search by applicant name.
  - Status filter dropdown.
  - Pagination (10 per page).
  - Review action links to `/admin/review/{applicationId}`.

- `src/pages/admin/AdminReviewPage.jsx`
  - Parallel load:
    - `/gateway/admin/applications/{id}`
    - `/gateway/admin/applications/{id}/history`
    - `/gateway/documents/application/{id}`
  - Document verification/rejection:
    - PUT `/gateway/admin/documents/{documentId}/verify`
  - Decision workflow:
    - POST `/gateway/admin/applications/{id}/decision`
  - Decision panel rules:
    - Decision allowed only in `DOCS_VERIFIED` or `UNDER_REVIEW`.
    - Remarks required.
    - Confirmation modal before submit.
  - File viewing:
    - Images in modal with Download + X.
    - PDFs open in new tab.

### Admin Route/Layout Support
- `src/App.jsx` updated to use new admin pages under `src/pages/admin/*`.
- Added route `/admin/review/:id`.
- `src/layouts/AdminLayout.jsx` users nav item removed.

## Status Badge Normalization
- `src/components/StatusBadge.jsx` expanded to support:
  - Title case statuses.
  - Uppercase underscore statuses.
  - Document-related statuses.
  - `CLOSED`.

## Build/Validation Status
- Frontend build repeatedly validated using:
  - `npm run build`
- Latest state after admin pages creation:
  - Build successful (Vite production build passed).

## Known Conventions and Decisions
- Base gateway URL used by frontend: `http://localhost:8002`.
- Role convention preference: uppercase role values (`APPLICANT`, `ADMIN`).
- API response parsing is defensive because endpoints return varied shapes:
  - raw arrays
  - `{ items: [...] }`
  - `{ data: [...] }`
  - nested `{ data: { ... } }`

## Open Risks / Things to Watch
- Some legacy placeholder admin files may still exist in `src/pages/` (outside `src/pages/admin/`).
- Ensure backend status enums remain aligned with StatusBadge mappings.
- Verify CORS and auth header behavior for document file viewing in all environments.

## Quick Resume Checklist (Before Next Frontend Task)
1. Run `npm install` if dependencies changed.
2. Run `npm run build` and ensure no compile issues.
3. Confirm auth/session behavior after refresh.
4. Validate applicant document preview/upload flow.
5. Validate admin review flow (verify/reject docs and approve/reject application).
6. Append any new endpoint/path/schema changes to this file immediately.

## Update Log Format (Use This for Future Entries)
When adding a new section, append:
- Date:
- Area:
- Files changed:
- Endpoints touched:
- Behavior changed:
- Validation done:
- Follow-up required:
