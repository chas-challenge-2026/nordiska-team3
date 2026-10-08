export type TransactionType = 'deposit' | 'withdrawal' | 'transfer' | 'interest'

export interface HistoryTransaction {
    id: string
    title: string
    accountName: string
    date: string
    month: string
    amount: number
    type: TransactionType
}