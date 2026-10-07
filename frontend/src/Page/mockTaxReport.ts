export type TaxReportStatus = 'pending' | 'ready' | 'failed'

export interface TaxAccountBreakdown {
    id: string
    name: string
    interest: number
    color: 'green' | 'orange' | 'purple'
}

export interface TaxReportYear {
    year: number
    status: TaxReportStatus
    totalInterest: number
    taxRate: number
    accounts: TaxAccountBreakdown[]
}

export const mockTaxReports: TaxReportYear[] = [
    {
        year: 2024,
        status: 'ready',
        totalInterest: 616.55,
        taxRate: 0.3,
        accounts: [
            { id: '1', name: 'Högskolesparande', interest: 120.5, color: 'green' },
            { id: '2', name: 'Semesterfonden', interest: 87.3, color: 'orange' },
            { id: '3', name: 'Buffertsparande', interest: 408.75, color: 'purple' },
        ],
    },
    {
        year: 2023,
        status: 'ready',
        totalInterest: 502.1,
        taxRate: 0.3,
        accounts: [
            { id: '1', name: 'Högskolesparande', interest: 95.4, color: 'green' },
            { id: '2', name: 'Semesterfonden', interest: 61.2, color: 'orange' },
            { id: '3', name: 'Buffertsparande', interest: 345.5, color: 'purple' },
        ],
    },
]