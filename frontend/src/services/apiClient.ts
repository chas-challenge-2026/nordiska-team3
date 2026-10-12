import { clearStoredAuth, getStoredAccessToken, storeLoginResponse, type LoginResponse } from './authService'

const API_URL = import.meta.env.VITE_API_URL

let refreshRequest: Promise<boolean> | null = null

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

async function refreshAccessToken() {
    const response = await fetch(`${API_URL}/api/auth/refresh`, {
        method: 'POST',
        credentials: 'include',
    })

    if (!response.ok) {
        return false
    }

    const data: LoginResponse = await response.json()
    storeLoginResponse(data)

    return true
}

async function getFreshAccessToken() {
    refreshRequest ??= refreshAccessToken().finally(() => {
        refreshRequest = null
    })

    return refreshRequest
}

/**
 * Fetch wrapper for authenticated API calls.
 * On a 401 it refreshes the access token once, retries the original request,
 * and sends the user to login if refresh is unavailable or expired.
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

    const didRefresh = await getFreshAccessToken()

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
