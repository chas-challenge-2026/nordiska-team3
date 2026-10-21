const API_URL = import.meta.env.VITE_API_URL

export type LoginResponse = {
    user: {
        id: string
        name: string
        email: string
        personalNumber: string
    }
    accessToken: string
}

// Token och användare finns bara i minnet. Refresh-token-cookien (HttpOnly) återställer sessionen vid omladdning.
let accessToken: string | null = null
let currentUser: LoginResponse['user'] | null = null
let refreshRequest: Promise<boolean> | null = null

// Rensa gamla värden från tidigare versioner som sparade i localStorage.
localStorage.removeItem('accessToken')
localStorage.removeItem('user')

/** Sparar token och användare i minnet efter login eller refresh. */
export function storeLoginResponse(data: LoginResponse) {
    accessToken = data.accessToken
    currentUser = data.user
}

/** Rensar inloggningen i minnet. */
export function clearStoredAuth() {
    accessToken = null
    currentUser = null
}

async function requestRefresh() {
    try {
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
    } catch {
        return false
    }
}

/**
 * Hämtar ny token och användare via refresh-cookien.
 * Samtidiga anrop delar samma request (t.ex. StrictMode som kör effekter två gånger).
 */
export function refreshSession(): Promise<boolean> {
    refreshRequest ??= requestRefresh().finally(() => {
        refreshRequest = null
    })

    return refreshRequest
}

/** Loggar in med personnummer och PIN. */
export async function login(
    personalNumber: string,
    pin: string
): Promise<LoginResponse> {
    const response = await fetch(`${API_URL}/api/auth/login-pin`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include', // krävs för att ta emot HttpOnly refresh-token-cookien
        body: JSON.stringify({ personalNumber, pin }),
    })

    if (!response.ok) {
        const errorData = await response.json().catch(() => null)
        throw new Error(errorData?.message ?? 'Felaktigt personnummer eller PIN-kod.')
    }

    const data: LoginResponse = await response.json()
    storeLoginResponse(data)

    return data
}

/** Loggar ut och rensar alltid lokal inloggning. */
export async function logout() {
    // Spara token innan vi rensar, så backend kan identifiera användaren.
    const token = getStoredAccessToken()

    try {
        // Backend-logouten kräver JWT i Authorization-headern och refresh-token-cookien via credentials.
        await fetch(`${API_URL}/api/auth/logout`, {
            method: 'POST',
            headers: token
                ? { Authorization: `Bearer ${token}` }
                : {},
            credentials: 'include',
        })
    } catch {
        // Även om backend-anropet misslyckas ska användaren loggas ut lokalt.
    }

    // Rensa inloggningsdata i minnet.
    clearStoredAuth()
}

/** Hämtar inloggad användare om den finns. */
export function getStoredUser(): LoginResponse['user'] | null {
    return currentUser
}

/** Hämtar access-token från minnet. */
export function getStoredAccessToken(): string | null {
    return accessToken
}

/** Kollar om användaren verkar vara inloggad. */
export function isAuthenticated(): boolean {
    return Boolean(getStoredAccessToken())
}
