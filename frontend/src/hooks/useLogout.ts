import { useNavigate } from 'react-router-dom'
import { logout } from '../services/authService'

export function useLogout() {
    const navigate = useNavigate()

    async function handleLogout() {
        // Rensa sparad inloggningsdata och försök meddela backend.
        await logout()

        // Skicka användaren tillbaka till login-sidan efter logout.
        navigate('/login')
    }

    return handleLogout
}