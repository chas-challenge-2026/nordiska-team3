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