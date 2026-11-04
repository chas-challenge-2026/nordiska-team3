import { useLocation, useNavigate } from 'react-router-dom'

export type LinkedModal = 'createAccount' | 'profileSettings'

/**
 * Läser om en länk vill öppna en modal på sidan.
 */
export function useModalFromLink(modal: LinkedModal) {
    const location = useLocation()
    const navigate = useNavigate()

    const isRequested = (location.state as { openModal?: LinkedModal } | null)?.openModal === modal

    /** Rensar modal-önskemålet från länken. */
    function clearRequest() {
        if (!isRequested) return

        navigate(
            { pathname: location.pathname, search: location.search, hash: location.hash },
            { replace: true, state: null }
        )
    }

    return { isRequested, clearRequest }
}
