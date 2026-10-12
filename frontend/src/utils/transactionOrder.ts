import type { AccountWithTransactions } from '../services/accountOverviewService'
import type { BackendTransaction } from '../services/accountService'

/** Hämtar datumet som ska användas vid sortering. */
function getTransactionTime(transaction: BackendTransaction) {
    return new Date(transaction.completedAt ?? transaction.createdAt).getTime()
}

/**
 * Slår ihop alla kontons transaktioner och sorterar nyast först.
 */
export function getTransactionsNewestFirst(groups: AccountWithTransactions[]) {
    return groups
        .flatMap(({ account, history }) => history.transactions.map((transaction) => ({ account, transaction })))
        .sort((a, b) => getTransactionTime(b.transaction) - getTransactionTime(a.transaction))
}
