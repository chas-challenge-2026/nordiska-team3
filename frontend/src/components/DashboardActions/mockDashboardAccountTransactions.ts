export type DashboardAccountTransactionType = 'deposit' | 'withdrawal' | 'interest'

export type DashboardAccountTransaction = {
    id: string
    accountId: string
    title: string
    date: string
    amount: number
    type: DashboardAccountTransactionType
}

export const mockDashboardAccountTransactions: DashboardAccountTransaction[] = [
    {
        id: 'education-deposit-1',
        accountId: 'education',
        title: 'Månatlig insättning',
        date: '15 juni 2024',
        amount: 5000,
        type: 'deposit',
    },
    {
        id: 'education-interest-1',
        accountId: 'education',
        title: 'Månadsränta',
        date: '1 juni 2024',
        amount: 120.5,
        type: 'interest',
    },
    {
        id: 'education-withdrawal-1',
        accountId: 'education',
        title: 'Uttag till bankkonto',
        date: '14 mars 2024',
        amount: -500,
        type: 'withdrawal',
    },
    {
        id: 'vacation-withdrawal-1',
        accountId: 'vacation',
        title: 'Uttag till resa',
        date: '28 maj 2024',
        amount: -2000,
        type: 'withdrawal',
    },
    {
        id: 'vacation-deposit-1',
        accountId: 'vacation',
        title: 'Extra insättning',
        date: '20 feb. 2024',
        amount: 2000,
        type: 'deposit',
    },
    {
        id: 'buffer-deposit-1',
        accountId: 'buffer',
        title: 'Extra insättning',
        date: '20 maj 2024',
        amount: 3500,
        type: 'deposit',
    },
    {
        id: 'buffer-interest-1',
        accountId: 'buffer',
        title: 'Månadsränta',
        date: '1 maj 2024',
        amount: 198.75,
        type: 'interest',
    },
]