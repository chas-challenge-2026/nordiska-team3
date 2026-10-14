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

/** Sparar token och användare efter login eller refresh. */
export function storeLoginResponse(data: LoginResponse) {
    localStorage.setItem('accessToken', data.accessToken)
    localStorage.setItem('user', JSON.stringify(data.user))
}

/** Rensar sparad inloggning. */
export function clearStoredAuth() {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('user')
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
    // Spara token innan vi rensar localStorage, så backend kan identifiera användaren.
    const accessToken = getStoredAccessToken()

    try {
        // Backend-logouten kräver JWT i Authorization-headern och refresh-token-cookien via credentials.
        await fetch(`${API_URL}/api/auth/logout`, {
            method: 'POST',
            headers: accessToken
                ? { Authorization: `Bearer ${accessToken}` }
                : {},
            credentials: 'include',
        })
    } catch {
        // Även om backend-anropet misslyckas ska användaren loggas ut lokalt.
    }

    // Rensa frontendens sparade inloggningsdata.
    clearStoredAuth()
}

/** Hämtar sparad användare om den finns. */
export function getStoredUser(): LoginResponse['user'] | null {
    const storedUser = localStorage.getItem('user')
    if (!storedUser || storedUser === 'undefined') return null
    try {
        return JSON.parse(storedUser) as LoginResponse['user']
    } catch {
        return null
    }
}

/** Hämtar sparad access-token. */
export function getStoredAccessToken(): string | null {
    return localStorage.getItem('accessToken')
}

/** Kollar om användaren verkar vara inloggad. */
export function isAuthenticated(): boolean {
    return Boolean(getStoredAccessToken())
}
