import { useLocation, useNavigate } from 'react-router-dom'

export type LinkedModal = 'createAccount' | 'profileSettings'

/**
 * Lets links request that a page opens a modal through router state.
 * Example: `<Link to="/dashboard" state={{ openModal: 'createAccount' }}>`.
 */
export function useModalFromLink(modal: LinkedModal) {
    const location = useLocation()
    const navigate = useNavigate()

    const isRequested = (location.state as { openModal?: LinkedModal } | null)?.openModal === modal

    /** Clears the router state so the modal does not reopen on reload or back navigation. */
    function clearRequest() {
        if (!isRequested) return

        navigate(
            { pathname: location.pathname, search: location.search, hash: location.hash },
            { replace: true, state: null }
        )
    }

    return { isRequested, clearRequest }
}
