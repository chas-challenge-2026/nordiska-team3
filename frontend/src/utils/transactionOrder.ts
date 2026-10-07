import type { AccountWithTransactions } from '../services/accountOverviewService'
import type { BackendTransaction } from '../services/accountService'

// Samma datum som visas i gränssnittet: när transaktionen genomfördes, annars när den skapades
function getTransactionTime(transaction: BackendTransaction) {
    return new Date(transaction.completedAt ?? transaction.createdAt).getTime()
}

// Backend skickar transaktioner per konto. Utan sortering hamnar alla transaktioner
// från första kontot före nyare transaktioner från nästa konto.
export function getTransactionsNewestFirst(groups: AccountWithTransactions[]) {
    return groups
        .flatMap(({ account, history }) => history.transactions.map((transaction) => ({ account, transaction })))
        .sort((a, b) => getTransactionTime(b.transaction) - getTransactionTime(a.transaction))
}
