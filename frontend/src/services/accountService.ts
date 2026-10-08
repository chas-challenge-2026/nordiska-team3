import { getStoredAccessToken } from './authService'

const API_URL = import.meta.env.VITE_API_URL

export type BackendAccount = {
    id: string
    accountNumber: string
    accountType: string
    name: string
    status: string
    balance: string
    // Skickas inte av backend än, se utils/accountInterest.ts
    interestRate?: string
    interestType?: 'VARIABLE' | 'FIXED'
    // Skickas inte av backend än, se utils/nextEvent.ts
    nextInterestPayoutDate?: string
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

async function getErrorMessage(response: Response, fallbackMessage: string): Promise<string> {
    try {
        const data: unknown = await response.json()

        if (
            data &&
            typeof data === 'object' &&
            'message' in data &&
            typeof data.message === 'string'
        ) {
            return data.message
        }
    } catch {
        // Use fallback message when the API does not return JSON.
    }

    return fallbackMessage
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

export async function createAccount(accountType: string): Promise<BackendAccount> {
    const response = await fetch(`${API_URL}/api/accounts`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders(),
        },
        credentials: 'include',
        body: JSON.stringify({ accountType }),
    })

    if (!response.ok) {
        throw new Error('Kunde inte skapa konto.')
    }

    return response.json()
}

export async function renameAccount(accountId: string, name: string): Promise<BackendAccount> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/name`, {
        method: 'PATCH',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders(),
        },
        credentials: 'include',
        body: JSON.stringify({ name }),
    })

    if (!response.ok) {
        throw new Error('Kunde inte namnge konto.')
    }

    return response.json()
}

export type TransactionResult = {
    transactionId: string
    balance: string
}

export type TransferResult = {
    transferId: string
    fromBalance: string
    toBalance: string | null
}

export type RecipientLookup = {
    accountNumber: string
    ownerName: string
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

export async function transfer(
    accountId: string,
    toAccountNumber: string,
    amount: number
): Promise<TransferResult> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/transfer`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...getAuthHeaders(),
        },
        credentials: 'include',
        body: JSON.stringify({ toAccountNumber, amount }),
    })

    if (!response.ok) {
        throw new Error(await getErrorMessage(response, 'Överföringen misslyckades.'))
    }

    return response.json()
}

export async function lookupAccount(accountNumber: string): Promise<RecipientLookup> {
    const response = await fetch(
        `${API_URL}/api/accounts/lookup?accountNumber=${encodeURIComponent(accountNumber)}`,
        {
            method: 'GET',
            headers: getAuthHeaders(),
            credentials: 'include',
        }
    )

    if (!response.ok) {
        throw new Error('Kontot kunde inte hittas.')
    }

    return response.json()
}

export type BackendTransaction = {
    id: string
    accountId?: string
    transactionType: string
    amount: string
    status: string
    createdAt: string
    completedAt: string | null
    transferId?: string | null
    counterparty?: string | null
    // Skickas inte av backend än, se utils/nextEvent.ts
    expectedCompletionDate?: string | null
}

export type TransactionHistoryResponse = {
    transactions: BackendTransaction[]
    page: number
    pageSize: number
    totalCount: number
    totalPages: number
}

export async function getTransactionsForAccount(
    accountId: string,
    page = 1,
    pageSize = 20
): Promise<TransactionHistoryResponse> {
    const response = await fetch(`${API_URL}/api/accounts/${accountId}/transactions?page=${page}&pageSize=${pageSize}`, {
        method: 'GET',
        headers: getAuthHeaders(),
        credentials: 'include',
    })

    if (!response.ok) {
        throw new Error('Kunde inte hämta transaktioner.')
    }

    return response.json()
}

export type TransactionFilters = {
    accountId?: string
    type?: string
    from?: string
    to?: string
}

export async function getTransactions(
    page = 1,
    pageSize = 20,
    filters: TransactionFilters = {}
): Promise<TransactionHistoryResponse> {
    const searchParams = new URLSearchParams({
        page: String(page),
        pageSize: String(pageSize),
    })

    if (filters.accountId) {
        searchParams.set('accountId', filters.accountId)
    }

    if (filters.type) {
        searchParams.set('type', filters.type)
    }

    if (filters.from) {
        searchParams.set('from', filters.from)
    }

    if (filters.to) {
        searchParams.set('to', filters.to)
    }

    const response = await fetch(`${API_URL}/api/transactions?${searchParams.toString()}`, {
        method: 'GET',
        headers: getAuthHeaders(),
        credentials: 'include',
    })

    if (!response.ok) {
        throw new Error('Kunde inte hämta transaktioner.')
    }

    return response.json()
}
