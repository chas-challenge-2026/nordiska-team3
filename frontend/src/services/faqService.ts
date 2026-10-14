import { apiFetch } from './apiClient'

const API_URL = import.meta.env.VITE_API_URL

// En fråga från GET /api/faq
export type FaqEntry = {
    id: string
    question: string
    answer: string
    category: string
}

// Svar från GET /api/faq/search. Använd matchFound, inte score, för att avgöra om det finns ett svar.
// När matchFound är false innehåller message backendens text om hur man når kundtjänst.
export type FaqSearchResult = {
    matchFound: boolean
    score: number
    question: string | null
    answer: string | null
    category: string | null
    message: string | null
}

export async function getFaqEntries(): Promise<FaqEntry[]> {
    const response = await apiFetch(`${API_URL}/api/faq`)

    if (!response.ok) {
        throw new Error('Kunde inte hämta vanliga frågor.')
    }

    return response.json()
}

export async function searchFaq(query: string): Promise<FaqSearchResult> {
    const response = await apiFetch(`${API_URL}/api/faq/search?q=${encodeURIComponent(query)}`)

    if (!response.ok) {
        throw new Error('Sökningen misslyckades.')
    }

    return response.json()
}
