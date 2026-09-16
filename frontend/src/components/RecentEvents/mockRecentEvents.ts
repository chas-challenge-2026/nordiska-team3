export type RecentEvent = {
    id: string
    title: string
    accountName: string
    date: string
    amount: number
    type: 'deposit' | 'withdrawal' | 'interest'
}

export const mockRecentEvents: RecentEvent[] = [
    {
        id: '1',
        title: 'Månatlig insättning',
        accountName: 'Högskolesparande',
        date: '20 jan. 2024',
        amount: 5000,
        type: 'deposit',
    },
    {
        id: '2',
        title: 'Månadsränta',
        accountName: 'Buffertsparande',
        date: '15 jan. 2024',
        amount: 221.4,
        type: 'interest',
    },
    {
        id: '3',
        title: 'Uttag till bankkonto',
        accountName: 'Semesterfonden',
        date: '10 jan. 2024',
        amount: -2000,
        type: 'withdrawal',
    },
    {
        id: '4',
        title: 'Extra insättning',
        accountName: 'Buffertsparande',
        date: '5 jan. 2024',
        amount: 3000,
        type: 'deposit',
    },
    {
        id: '5',
        title: 'Månadsränta',
        accountName: 'Högskolesparande',
        date: '28 dec. 2023',
        amount: 218.3,
        type: 'interest',
    },
]