import { type TransactionType } from './mockHistoryTransaction'
import { UserProfile } from '../components/UserProfile'
import './HistoryPage.css'
import { useState } from 'react'
import { type BackendAccount, type BackendTransaction } from '../services/accountService'
import { useAccountsWithTransactions } from '../hooks/useAccountsWithTransactions'
import { ArrowDownLeft, ArrowUpRight } from 'lucide-react'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { useLogout } from '../hooks/useLogout'
import { useTheme } from '../context/useTheme'

type HistoryFilter = 'all' | 'deposit' | 'withdrawal' | 'interest'
type AccountFilter = 'all' | string

const historyFilters: { value: HistoryFilter; label: string }[] = [
    { value: 'all', label: 'Alla' },
    { value: 'deposit', label: 'Insättningar' },
    { value: 'withdrawal', label: 'Uttag' },
    { value: 'interest', label: 'Ränta' },
]

function formatTransactionAmount(amount: number) {
    const sign = amount > 0 ? '+' : ''

    return `${sign}${amount.toLocaleString('sv-SE', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })} kr`
}

function getTransactionDirection(amount: number) {
    return amount < 0 ? 'negative' : 'positive'
}

function getTransactionIcon(amount: number) {
    return amount < 0 ? <ArrowUpRight size={14} strokeWidth={2.6} /> : <ArrowDownLeft size={14} strokeWidth={2.6} />
}

function getTransactionLabel(type: TransactionType) {
    if (type === 'deposit') {
        return 'Insättning'
    }

    if (type === 'withdrawal') {
        return 'Uttag'
    }

    return 'Ränta'
}

function parseBackendAmount(amount: string) {
    return Number(amount.replace(/\s/g, '').replace(',', '.'))
}

function mapBackendTransactionType(transactionType: string): TransactionType {
    if (transactionType === 'DEPOSIT') {
        return 'deposit'
    }

    if (transactionType === 'WITHDRAWAL') {
        return 'withdrawal'
    }

    return 'interest'
}

function formatTransactionDate(date: string) {
    return new Intl.DateTimeFormat('sv-SE', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
    }).format(new Date(date))
}

function formatTransactionMonth(date: string) {
    const formatted = new Intl.DateTimeFormat('sv-SE', {
        month: 'long',
        year: 'numeric',
    }).format(new Date(date))

    return formatted.charAt(0).toUpperCase() + formatted.slice(1)
}

function getTransactionTitle(type: TransactionType) {
    if (type === 'deposit') {
        return 'Insättning'
    }

    if (type === 'withdrawal') {
        return 'Uttag'
    }

    return 'Ränta'
}

function mapBackendTransactionToHistoryTransaction(
    transaction: BackendTransaction,
    account: BackendAccount
) {
    const type = mapBackendTransactionType(transaction.transactionType)
    const rawAmount = parseBackendAmount(transaction.amount)
    const amount = type === 'withdrawal' ? -Math.abs(rawAmount) : rawAmount
    const date = transaction.completedAt ?? transaction.createdAt

    return {
        id: transaction.id,
        title: getTransactionTitle(type),
        accountName: account.name,
        date: formatTransactionDate(date),
        month: formatTransactionMonth(date),
        amount,
        type,
    }
}

function HistoryPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()
    const [activeFilter, setActiveFilter] = useState<HistoryFilter>('all')
    const [activeAccount, setActiveAccount] = useState<AccountFilter>('all')
    const { data: accountTransactionGroups, isLoading: isLoadingTransactions, isError: transactionsError } = useAccountsWithTransactions()
    const transactions = (accountTransactionGroups ?? []).flatMap(({ account, transactions: groupTransactions }) =>
        groupTransactions.map((transaction) => mapBackendTransactionToHistoryTransaction(transaction, account))
    )

    const accountFilters = Array.from(new Set(transactions.map((transaction) => transaction.accountName)))

    const visibleTransactions = transactions.filter((transaction) => {
        const matchesType = activeFilter === 'all' || transaction.type === activeFilter
        const matchesAccount = activeAccount === 'all' || transaction.accountName === activeAccount

        return matchesType && matchesAccount
    })

    function handleAccountChange(accountName: AccountFilter) {
        setActiveAccount(accountName)
    }

    function handleTypeChange(filter: HistoryFilter) {
        setActiveFilter(filter)
    }

    const selectedAccountLabel = activeAccount === 'all' ? 'Alla konton' : activeAccount

    const groupedTransactions = visibleTransactions.reduce<Record<string, typeof transactions>>(
        (groups, transaction) => {
            if (!groups[transaction.month]) {
                groups[transaction.month] = []
            }

            groups[transaction.month].push(transaction)
            return groups
        },
        {}
    )

    return (
        <div className="history-page">
            <DecorativeCircle color="orange" size={150} left={-35} top={210} />
            <DecorativeCircle color="blue" size={120} left={55} top={390} opacity={0.92} />
            <DecorativeCircle color="orange" size={170} left={220} bottom={80} opacity={0.95} />
            <DecorativeCircle color="green" size={96} right={90} top={250} opacity={0.82} />
            <DecorativeCircle color="green" size={140} right={-30} top={360} opacity={0.82} />

            <div className="history-brand-mark brand-logo" aria-hidden="true">
                <span>Sparportal</span>
                <strong>
                    nordiska<span>.</span>
                </strong>
            </div>

            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <main className="history-main">
                <UserProfile />

                <section className="history-content">
                    <header className="history-header">
                        <h1 tabIndex={0}>Transaktionshistorik</h1>
                        <p>Alla rörelser på dina sparkonton.</p>
                    </header>

                    <div className="history-controls">
                        <div className="history-controls-desktop">
                            <label
                                className={`history-select-filter ${
                                    activeAccount !== 'all' ? 'history-select-filter--active' : ''
                                }`}
                            >
                                <span>Konto</span>
                                <select
                                    aria-label="Välj konto"
                                    value={activeAccount}
                                    onChange={(event) => handleAccountChange(event.target.value)}
                                >
                                    <option value="all">Alla konton</option>
                                    {accountFilters.map((accountName) => (
                                        <option value={accountName} key={accountName}>
                                            {accountName}
                                        </option>
                                    ))}
                                </select>
                            </label>

                            {historyFilters
                                .filter((filter) => filter.value !== 'all')
                                .map((filter) => (
                                    <button
                                        className={`history-filter ${
                                            activeFilter === filter.value ? 'history-filter--active' : ''
                                        }`}
                                        type="button"
                                        key={filter.value}
                                        aria-pressed={activeFilter === filter.value}
                                        onClick={() => handleTypeChange(activeFilter === filter.value ? 'all' : filter.value)}
                                    >
                                        {filter.label}
                                    </button>
                                ))}
                        </div>

                        <div className="history-controls-mobile">
                            <label
                                className={`history-select-filter ${
                                    activeAccount !== 'all' ? 'history-select-filter--active' : ''
                                }`}
                            >
                                <span>Konto</span>
                                <select
                                    aria-label="Välj konto"
                                    value={activeAccount}
                                    onChange={(event) => handleAccountChange(event.target.value)}
                                >
                                    <option value="all">Alla konton</option>
                                    {accountFilters.map((accountName) => (
                                        <option value={accountName} key={accountName}>
                                            {accountName}
                                        </option>
                                    ))}
                                </select>
                            </label>

                            <label
                                className={`history-select-filter ${
                                    activeFilter !== 'all' ? 'history-select-filter--active' : ''
                                }`}
                            >
                                <span>Typ</span>
                                <select
                                    aria-label="Välj transaktionstyp"
                                    value={activeFilter}
                                    onChange={(event) => handleTypeChange(event.target.value as HistoryFilter)}
                                >
                                    <option value="all">Alla typer</option>
                                    {historyFilters
                                        .filter((filter) => filter.value !== 'all')
                                        .map((filter) => (
                                            <option value={filter.value} key={filter.value}>
                                                {filter.label}
                                            </option>
                                        ))}
                                </select>
                            </label>
                        </div>
                    </div>

                    {isLoadingTransactions ? (
                        <div className="history-empty">
                            <h2 tabIndex={0}>Laddar transaktioner</h2>
                            <p>Hämtar historik för dina konton.</p>
                        </div>
                    ) : transactionsError ? (
                        <div className="history-empty">
                            <h2 tabIndex={0}>Historiken kunde inte hämtas</h2>
                            <p>{transactionsError}</p>
                        </div>
                    ) : visibleTransactions.length > 0 ? (
                        <div className="history-list">
                            {Object.entries(groupedTransactions).map(([month, transactions]) => (
                                <section className="history-month" key={month}>
                                    <h2 tabIndex={0}>{month}</h2>

                                    {transactions.map((transaction) => (
                                        <article className="transaction-card" key={transaction.id}>
                                            <div
                                                className={`transaction-icon transaction-icon--${getTransactionDirection(
                                                    transaction.amount
                                                )}`}
                                            >
                                                {getTransactionIcon(transaction.amount)}
                                            </div>

                                            <div className="transaction-info">
                                                <h3 tabIndex={0}>{transaction.title}</h3>
                                                <p>
                                                    {transaction.accountName} · {transaction.date}
                                                </p>
                                            </div>

                                            <div className="transaction-meta">
                                                <strong
                                                    className={`transaction-amount transaction-amount--${getTransactionDirection(
                                                        transaction.amount
                                                    )}`}
                                                >
                                                    {formatTransactionAmount(transaction.amount)}
                                                </strong>

                                                <span className={`transaction-badge transaction-badge--${transaction.type}`}>
                                                    {getTransactionLabel(transaction.type)}
                                                </span>
                                            </div>
                                        </article>
                                    ))}
                                </section>
                            ))}
                        </div>
                    ) : (
                        <div className="history-empty">
                            <h2 tabIndex={0}>Inga transaktioner hittades</h2>
                            <p>Det finns inga rörelser för {selectedAccountLabel} som matchar det valda filtret.</p>
                        </div>
                    )}
                </section>
            </main>
        </div>
    )
}

export default HistoryPage
