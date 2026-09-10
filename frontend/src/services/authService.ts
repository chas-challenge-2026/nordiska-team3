export type LoginResponse = {
    user: {
        id: string
        name: string
        email: string
        personalNumber: string
    }
    accessToken: string
}

// Den här funktionen fungerar som en tillfällig koppling mellan login-sidan
// och ett framtida login-API i backend. På så sätt kan frontend fortsätta
// byggas utan att vi behöver vänta på att backend är helt klar.
export async function login(
    personalNumber: string,
    pin: string
): Promise<LoginResponse> {
    // Simulerar tiden det hade tagit att skicka en inloggningsförfrågan till backend.
    await new Promise((resolve) => setTimeout(resolve, 900))

    if (!personalNumber || !pin) {
        throw new Error('Fyll i personnummer och PIN-kod.')
    }
    // Tillfällig mock-data som senare ska ersättas av svaret från backend.
    return {
        user: {
            id: 'mock-user-1',
            name: 'Emma Lindström',
            email: 'emma@exempel.se',
            personalNumber: personalNumber,
        },
        accessToken: 'mock-access-token',
    }
}

// Rensar frontendens sparade login-data.
// Senare kan vi också lägga till ett API-anrop här, till exempel POST /api/auth/logout.
export function logout() {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('user')
}

// Hämtar användaren som sparades vid login.
// Just nu läser vi från localStorage, men senare kan detta ersättas med GET /api/auth/me.
export function getStoredUser(): LoginResponse['user'] | null {
    const storedUser = localStorage.getItem('user')

    if (!storedUser) {
        return null
    }

    return JSON.parse(storedUser) as LoginResponse['user']
}

// Hämtar sparad accessToken från localStorage.
// Senare används den för att skicka Authorization-headern till skyddade API-anrop.
export function getStoredAccessToken(): string | null {
    return localStorage.getItem('accessToken')
}

// Kollar om det finns en sparad token.
// Senare kan detta göras mer avancerat, till exempel genom att kontrollera om token har gått ut.
export function isAuthenticated(): boolean {
    return Boolean(getStoredAccessToken())
}