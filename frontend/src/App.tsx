import { Navigate, Route, Routes } from 'react-router-dom'
import { isAuthenticated } from './services/authService'
import LoginPage from './Page/LoginPage'
import DashboardPage from './Page/DashboardPage'
import TransactPage from './Page/TransactPage'
import HistoryPage from './Page/HistoryPage'
import TaxPage from './Page/TaxPage'
import FaqPage from './Page/FaqPage'

function ProtectedRoute({ children }: { children: JSX.Element }) {
  // Kollar om användaren har en sparad token.
  // Just nu räcker det för mock-login, senare kan detta kopplas mot GET /api/auth/me.
  if (!isAuthenticated()) {
    return <Navigate to="/login" replace />
  }

  return children
}

function App() {
  return (
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
  )
}
export default App