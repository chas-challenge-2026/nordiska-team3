import { useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { isAuthenticated, logout } from '../services/authService'

const IDLE_TIMEOUT_MS = 15 * 60 * 1000
const CHECK_INTERVAL_MS = 15 * 1000
const ACTIVITY_EVENTS = ['mousemove', 'mousedown', 'keydown', 'touchstart', 'wheel', 'scroll'] as const

export const IDLE_LOGOUT_MESSAGE = 'Du loggades ut på grund av inaktivitet.'

/** Loggar ut användaren efter en tids inaktivitet (mus, tangenter, touch). */
export function useIdleLogout() {
    const navigate = useNavigate()

    useEffect(() => {
        let lastActivity = Date.now()

        function markActivity() {
            lastActivity = Date.now()
        }

        // Intervall i stället för en timeout som nollställs, så att även tid i viloläge räknas.
        const intervalId = window.setInterval(async () => {
            if (!isAuthenticated()) {
                markActivity()
                return
            }

            if (Date.now() - lastActivity < IDLE_TIMEOUT_MS) return

            markActivity()
            await logout()
            navigate('/login', { replace: true, state: { logoutMessage: IDLE_LOGOUT_MESSAGE } })
        }, CHECK_INTERVAL_MS)

        ACTIVITY_EVENTS.forEach((eventName) => {
            window.addEventListener(eventName, markActivity, { passive: true })
        })

        return () => {
            window.clearInterval(intervalId)
            ACTIVITY_EVENTS.forEach((eventName) => {
                window.removeEventListener(eventName, markActivity)
            })
        }
    }, [navigate])
}
