import { useLocation, useNavigate } from 'react-router-dom'

export type LinkedModal = 'createAccount' | 'profileSettings'

// En länk kan be en sida öppna en modal genom att skicka med state, t.ex.
// <Link to="/dashboard" state={{ openModal: 'createAccount' }}>. Används av FAQ-svaren.
export function useModalFromLink(modal: LinkedModal) {
    const location = useLocation()
    const navigate = useNavigate()

    const isRequested = (location.state as { openModal?: LinkedModal } | null)?.openModal === modal

    // Rensa state när modalen stängs, så att den inte öppnas igen vid omladdning eller bakåt-knappen
    function clearRequest() {
        if (!isRequested) return

        navigate(
            { pathname: location.pathname, search: location.search, hash: location.hash },
            { replace: true, state: null }
        )
    }

    return { isRequested, clearRequest }
}
