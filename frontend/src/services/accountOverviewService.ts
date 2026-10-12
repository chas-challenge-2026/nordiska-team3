import {
    getAccounts,
    getTransactionsForAccount,
    type BackendAccount,
    type TransactionHistoryResponse,
} from './accountService'

export type AccountWithTransactions = {
    account: BackendAccount
    history: TransactionHistoryResponse
}

/**
 * Fetches accounts and their transaction pages in one shared service call.
 * This keeps dashboard-style screens from duplicating per-account transaction fetches.
 */
export async function getAccountsWithTransactions(page = 1, pageSize = 20): Promise<AccountWithTransactions[]> {
    const accounts = await getAccounts()

    const accountTransactionGroups = await Promise.all(
        accounts.map(async (account) => ({
            account,
            history: await getTransactionsForAccount(account.id, page, pageSize),
        }))
    )

    return accountTransactionGroups
}
