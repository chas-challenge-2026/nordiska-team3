export type TransactionType = 'deposit' | 'withdrawal' | 'interest'

export interface HistoryTransaction {
    id: string
    title: string
    accountName: string
    date: string
    month: string
    amount: number
    type: TransactionType
}

export const mockHistoryTransactions: HistoryTransaction[] = [
    {
        id: 'june-deposit-1',
        title: 'Månatlig insättning',
        accountName: 'Högskolesparande',
        date: '15 juni 2024',
        month: 'Juni 2024',
        amount: 5000,
        type: 'deposit',
    },
    {
        id: 'june-interest-1',
        title: 'Månadsränta',
        accountName: 'Högskolesparande',
        date: '1 juni 2024',
        month: 'Juni 2024',
        amount: 120.5,
        type: 'interest',
    },
    {
        id: 'may-withdrawal-1',
        title: 'Uttag till bankkonto',
        accountName: 'Semesterfonden',
        date: '28 maj 2024',
        month: 'Maj 2024',
        amount: -2000,
        type: 'withdrawal',
    },
    {
        id: 'may-deposit-1',
        title: 'Extra insättning',
        accountName: 'Buffertsparande',
        date: '20 maj 2024',
        month: 'Maj 2024',
        amount: 3500,
        type: 'deposit',
    },
    {
        id: 'may-interest-1',
        title: 'Månadsränta',
        accountName: 'Buffertsparande',
        date: '1 maj 2024',
        month: 'Maj 2024',
        amount: 198.75,
        type: 'interest',
    },
    {
        id: 'april-deposit-1',
        title: 'Månatlig insättning',
        accountName: 'Högskolesparande',
        date: '18 apr. 2024',
        month: 'April 2024',
        amount: 8000,
        type: 'deposit',
    },
    {
        id: 'april-withdrawal-1',
        title: 'Uttag till bankkonto',
        accountName: 'Semesterfonden',
        date: '10 apr. 2024',
        month: 'April 2024',
        amount: -1500,
        type: 'withdrawal',
    },
    {
        id: 'april-interest-1',
        title: 'Månadsränta',
        accountName: 'Semesterfonden',
        date: '1 apr. 2024',
        month: 'April 2024',
        amount: 87.3,
        type: 'interest',
    },
    {
        id: 'march-deposit-1',
        title: 'Extra insättning',
        accountName: 'Buffertsparande',
        date: '22 mars 2024',
        month: 'Mars 2024',
        amount: 10000,
        type: 'deposit',
    },
    {
        id: 'march-withdrawal-1',
        title: 'Uttag till bankkonto',
        accountName: 'Högskolesparande',
        date: '14 mars 2024',
        month: 'Mars 2024',
        amount: -500,
        type: 'withdrawal',
    },
    {
        id: 'march-interest-1',
        title: 'Månadsränta',
        accountName: 'Buffertsparande',
        date: '1 mars 2024',
        month: 'Mars 2024',
        amount: 210,
        type: 'interest',
    },
    {
        id: 'february-deposit-1',
        title: 'Månatlig insättning',
        accountName: 'Semesterfonden',
        date: '20 feb. 2024',
        month: 'Februari 2024',
        amount: 2000,
        type: 'deposit',
    },
]
