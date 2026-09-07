import { Navigate, Route, Routes } from 'react-router-dom'
import LoginPage from './Page/LoginPage'
import DashboardPage from './Page/DashboardPage'

function App() {
  return (
    <Routes>
      <Route path="/" element={<Navigate to="/login" replace />} />
      <Route path="/login" element={<LoginPage />} />
      <Route path="/dashboard" element={<DashboardPage />} />
    </Routes>
  )
}
export default App