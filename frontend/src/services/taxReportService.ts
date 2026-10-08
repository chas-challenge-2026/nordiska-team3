import { getStoredAccessToken } from './authService'

const API_URL = import.meta.env.VITE_API_URL

export type TaxReportStatus = 'QUEUED' | 'PROCESSING' | 'READY' | 'FAILED'

// En rapport från GET /api/taxreports
export type TaxReport = {
    id: string
    reportYear: number
    status: TaxReportStatus
    createdAt: string
    completedAt: string | null
    errorMessage: string | null
    // Signeringen är inte inkopplad i backend än, så den är false tills vidare
    signed: boolean
    // PDF:en ligger på serverns disk, så en klar rapport kan sakna fil
    pdfAvailable: boolean
}

function getAuthHeaders(): Record<string, string> {
    const token = getStoredAccessToken()

    return token ? { Authorization: `Bearer ${token}` } : {}
}

// Backend sorterar nyaste år först, och inom samma år nyaste rapport först
export async function getTaxReports(): Promise<TaxReport[]> {
    const response = await fetch(`${API_URL}/api/taxreports`, {
        headers: getAuthHeaders(),
        // Statusen ändras medan rapporten genereras, så svaret får aldrig komma från webbläsarens cache
        cache: 'no-store',
    })

    if (!response.ok) {
        throw new Error('Kunde inte hämta skatterapporter.')
    }

    const data: { reports: TaxReport[] } = await response.json()

    return data.reports
}

export async function createTaxReport(reportYear: number): Promise<void> {
    const response = await fetch(`${API_URL}/api/taxreports`, {
        method: 'POST',
        headers: {
            ...getAuthHeaders(),
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({ reportYear }),
    })

    if (response.status === 429) {
        throw new Error('Du har begärt för många rapporter. Vänta en stund och försök igen.')
    }

    if (!response.ok) {
        throw new Error('Rapporten kunde inte begäras.')
    }
}

// Endpointen kräver token, så PDF:en hämtas med fetch och sparas via en tillfällig länk
export async function downloadTaxReport(report: TaxReport): Promise<void> {
    const response = await fetch(`${API_URL}/api/taxreports/${report.id}/download`, {
        headers: getAuthHeaders(),
    })

    if (!response.ok) {
        throw new Error('Rapporten kunde inte laddas ner.')
    }

    const blob = await response.blob()
    const url = URL.createObjectURL(blob)

    const link = document.createElement('a')
    link.href = url
    link.download = `skatterapport-${report.reportYear}.pdf`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
}
