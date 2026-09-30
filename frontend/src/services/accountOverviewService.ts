// Backend saknar just nu en endpoint för alla transaktioner per användare.
// Därför hämtar frontend transaktioner per konto här, samlat på ett ställe,
// så DashboardPage och HistoryPage slipper duplicera samma fetch-logik.

import {
    getAccounts,
    getTransactionsForAccount,
    type BackendAccount,
    type BackendTransaction,
} from './accountService'

export type AccountWithTransactions = {
    account: BackendAccount
    transactions: BackendTransaction[]
}

export async function getAccountsWithTransactions(): Promise<AccountWithTransactions[]> {
    const accounts = await getAccounts()

    const accountTransactionGroups = await Promise.all(
        accounts.map(async (account) => ({
            account,
            transactions: await getTransactionsForAccount(account.id),
        }))
    )

    return accountTransactionGroups
}
