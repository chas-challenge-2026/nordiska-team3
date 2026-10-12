import type { AccountWithTransactions } from '../services/accountOverviewService'
import type { BackendTransaction } from '../services/accountService'

/** Uses the same timestamp priority as the UI: completed date first, created date as fallback. */
function getTransactionTime(transaction: BackendTransaction) {
    return new Date(transaction.completedAt ?? transaction.createdAt).getTime()
}

/**
 * Flattens account-scoped transaction pages and sorts them newest first across all accounts.
 */
export function getTransactionsNewestFirst(groups: AccountWithTransactions[]) {
    return groups
        .flatMap(({ account, history }) => history.transactions.map((transaction) => ({ account, transaction })))
        .sort((a, b) => getTransactionTime(b.transaction) - getTransactionTime(a.transaction))
}
