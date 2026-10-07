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

// Riktigt API-anrop mot backend: POST /api/auth/login-pin
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

    localStorage.setItem('accessToken', data.accessToken)
    localStorage.setItem('user', JSON.stringify(data.user))

    return data
}

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
    localStorage.removeItem('accessToken')
    localStorage.removeItem('user')
}

// Hämtar användaren som sparades vid login.
export function getStoredUser(): LoginResponse['user'] | null {
    const storedUser = localStorage.getItem('user')
    if (!storedUser || storedUser === 'undefined') return null
    try {
        return JSON.parse(storedUser) as LoginResponse['user']
    } catch {
        return null
    }
}

// Hämtar sparad accessToken från localStorage.
export function getStoredAccessToken(): string | null {
    return localStorage.getItem('accessToken')
}

// Kollar om det finns en sparad token.
export function isAuthenticated(): boolean {
    return Boolean(getStoredAccessToken())
}