import { getStoredAccessToken } from './authService'

const API_URL = import.meta.env.VITE_API_URL

export type BackendAccount = {
    id: string
    accountNumber: string
    accountType: string
    name: string
    status: string
    balance: string
}

export type AccountsResponse = {
    accounts: BackendAccount[]
}

function getAuthHeaders(): Record<string, string> {
    const token = getStoredAccessToken()

    if (!token) {
        return {}
    }

    return {
        Authorization: `Bearer ${token}`,
    }
}

export async function getAccounts(): Promise<BackendAccount[]> {
    const response = await fetch(`${API_URL}/api/accounts`, {
        method: 'GET',
        headers: getAuthHeaders(),
        credentials: 'include',
    })

    if (!response.ok) {
        throw new Error('Kunde inte hämta konton.')
    }

    const data: AccountsResponse = await response.json()
    return data.accounts
}

export type TransactionResult = {
    transactionId: string
    balance: string
}

export async function deposit(accountId: string, amount: number): Promise<TransactionResult> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/deposit`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders(),
        },
        credentials: 'include',
        body: JSON.stringify({ amount }),
    })

    if (!response.ok) {
        throw new Error('Insättningen misslyckades.')
    }

    return response.json()
}

export async function withdraw(accountId: string, amount: number): Promise<TransactionResult> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/withdraw`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders(),
        },
        credentials: 'include',
        body: JSON.stringify({ amount }),
    })

    if (!response.ok) {
        throw new Error('Uttaget misslyckades.')
    }

    return response.json()
}

export type BackendTransaction = {
    id: string
    transactionType: string
    amount: string
    status: string
    createdAt: string
    completedAt: string | null
}

export type TransactionHistoryResponse = {
    transactions: BackendTransaction[]
}

export async function getTransactionsForAccount(accountId: string): Promise<BackendTransaction[]> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/transactions`, {
        method: 'GET',
        headers: getAuthHeaders(),
        credentials: 'include',
    })

    if (!response.ok) {
        throw new Error('Kunde inte hämta transaktioner.')
    }

    const data: TransactionHistoryResponse = await response.json()
    return data.transactions
}