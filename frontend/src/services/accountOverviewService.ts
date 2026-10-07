// Backend saknar just nu en endpoint för alla transaktioner per användare.
// Därför hämtar frontend transaktioner per konto här, samlat på ett ställe,
// så DashboardPage och HistoryPage slipper duplicera samma fetch-logik.

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
