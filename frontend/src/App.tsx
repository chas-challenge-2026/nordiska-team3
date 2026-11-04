import { useEffect, useState } from 'react'
import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import { isAuthenticated, refreshSession } from './services/authService'
import { useIdleLogout } from './hooks/useIdleLogout'
import LoginPage from './Page/LoginPage'
import DashboardPage from './Page/DashboardPage'
import TransactPage from './Page/TransactPage'
import HistoryPage from './Page/HistoryPage'
import TaxPage from './Page/TaxPage'
import FaqPage from './Page/FaqPage'

function ProtectedRoute({ children }: { children: JSX.Element }) {
  // Token finns bara i minnet, så efter omladdning försöker vi återställa sessionen via refresh-cookien.
  const [status, setStatus] = useState<'checking' | 'authenticated' | 'unauthenticated'>(
    isAuthenticated() ? 'authenticated' : 'checking'
  )

  useEffect(() => {
    if (status !== 'checking') return

    let isActive = true

    refreshSession().then((didRefresh) => {
      if (isActive) setStatus(didRefresh ? 'authenticated' : 'unauthenticated')
    })

    return () => {
      isActive = false
    }
  }, [status])

  if (status === 'checking') {
    return (
      <div className="session-loading" role="status">
        <p>Laddar...</p>
      </div>
    )
  }

  if (status === 'unauthenticated') {
    return <Navigate to="/login" replace />
  }

  return children
}

function ScrollToTop() {
  const { pathname } = useLocation()

  useEffect(() => {
    window.scrollTo({ top: 0, left: 0, behavior: 'auto' })
  }, [pathname])

  return null
}

function App() {
  useIdleLogout()

  return (
    <>
      <ScrollToTop />
      <Routes>
        <Route path="/" element={<Navigate to="/login" replace />} />
        <Route path="/login" element={<LoginPage />} />

        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <DashboardPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/transaktioner"
          element={
            <ProtectedRoute>
              <TransactPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/historik"
          element={
            <ProtectedRoute>
              <HistoryPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/skatt"
          element={
            <ProtectedRoute>
              <TaxPage />
            </ProtectedRoute>
          }
        />

        <Route
          path="/faq"
          element={
            <ProtectedRoute>
              <FaqPage />
            </ProtectedRoute>
          }
        />
      </Routes>
    </>
  )
}
export default App
