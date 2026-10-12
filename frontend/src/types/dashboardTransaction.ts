export type DashboardAccountTransactionType = 'deposit' | 'withdrawal' | 'transfer' | 'interest'

export type DashboardAccountTransaction = {
    id: string
    accountId: string
    title: string
    date: string
    amount: number
    type: DashboardAccountTransactionType
    counterparty?: string | null
}
