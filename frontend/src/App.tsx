import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from './Page/LoginPage'
import DashboardPage from './Page/DashboardPage'
import TransactPage from './Page/TransactPage'
import HistoryPage from './Page/HistoryPage'
import TaxPage from './Page/TaxPage'
import FaqPage from './Page/FaqPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={<DashboardPage />} />
      <Route path="/transaktioner" element={<TransactPage />} />
      <Route path="/historik" element={<HistoryPage />} />
      <Route path="/skatt" element={<TaxPage />} />
      <Route path="/faq" element={<FaqPage />} />
    </Routes>
  )
}
export default App