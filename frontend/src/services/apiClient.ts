import { clearStoredAuth, getStoredAccessToken, refreshSession } from './authService'

function withAuthHeaders(headers: HeadersInit | undefined) {
    const nextHeaders = new Headers(headers)
    const token = getStoredAccessToken()

    if (token) {
        nextHeaders.set('Authorization', `Bearer ${token}`)
    }

    return nextHeaders
}

function redirectToLogin() {
    clearStoredAuth()

    if (window.location.pathname !== '/login') {
        window.location.assign('/login')
    }
}

/**
 * Gör API-anrop med token.
 * Om token har gått ut testas refresh en gång.
 */
export async function apiFetch(input: RequestInfo | URL, init: RequestInit = {}) {
    const requestInit: RequestInit = {
        ...init,
        credentials: init.credentials ?? 'include',
        headers: withAuthHeaders(init.headers),
    }

    const response = await fetch(input, requestInit)

    if (response.status !== 401) {
        return response
    }

    const didRefresh = await refreshSession()

    if (!didRefresh) {
        redirectToLogin()
        return response
    }

    const retryResponse = await fetch(input, {
        ...requestInit,
        headers: withAuthHeaders(init.headers),
    })

    if (retryResponse.status === 401) {
        redirectToLogin()
    }

    return retryResponse
}
