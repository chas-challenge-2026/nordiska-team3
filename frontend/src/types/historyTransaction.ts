export type TransactionType = 'deposit' | 'withdrawal' | 'transfer' | 'interest'

export interface HistoryTransaction {
    id: string
    title: string
    accountName: string
    date: string
    rawDate: string
    month: string
    amount: number
    type: TransactionType
}
