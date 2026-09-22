export interface TaxAccountBreakdown {
    id: string
    name: string
    interest: number
    color: 'green' | 'orange' | 'purple'
}

export interface TaxReportYear {
    year: number
    totalInterest: number
    taxRate: number
    accounts: TaxAccountBreakdown[]
}

export const mockTaxReports: TaxReportYear[] = [
    {
        year: 2024,
        totalInterest: 616.55,
        taxRate: 0.3,
        accounts: [
            { id: 'education', name: 'Högskolesparande', interest: 120.5, color: 'green' },
            { id: 'vacation', name: 'Semesterfonden', interest: 87.3, color: 'orange' },
            { id: 'buffer', name: 'Buffertsparande', interest: 408.75, color: 'purple' },
        ],
    },
    {
        year: 2023,
        totalInterest: 402.1,
        taxRate: 0.3,
        accounts: [
            { id: 'education', name: 'Högskolesparande', interest: 90.2, color: 'green' },
            { id: 'vacation', name: 'Semesterfonden', interest: 61.4, color: 'orange' },
            { id: 'buffer', name: 'Buffertsparande', interest: 250.5, color: 'purple' },
        ],
    },
]