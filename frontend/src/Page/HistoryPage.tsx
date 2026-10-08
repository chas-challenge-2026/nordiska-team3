import { type TransactionType } from '../types/historyTransaction'
import { UserProfile } from '../components/UserProfile'
import './HistoryPage.css'
import { useState } from 'react'
import { useSearchParams } from 'react-router-dom'
import { useQuery } from '@tanstack/react-query'
import { getAccounts, getTransactions, type BackendAccount, type BackendTransaction } from '../services/accountService'
import { ArrowDownLeft, ArrowUpRight, ChevronLeft, ChevronRight } from 'lucide-react'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { useLogout } from '../hooks/useLogout'
import { useTheme } from '../context/useTheme'

type HistoryFilter = 'all' | 'deposit' | 'withdrawal' | 'transfer' | 'interest'
type AccountFilter = 'all' | string
type PeriodFilter = 'last30days' | 'thisYear' | 'all'

const historyFilters: { value: HistoryFilter; label: string }[] = [
    { value: 'all', label: 'Alla' },
    { value: 'deposit', label: 'Insättningar' },
    { value: 'withdrawal', label: 'Uttag' },
    { value: 'transfer', label: 'Överföringar' },
    { value: 'interest', label: 'Ränta' },
]

const periodFilters: { value: PeriodFilter; label: string }[] = [
    { value: 'last30days', label: 'Senaste 30 dagarna' },
    { value: 'thisYear', label: 'I år' },
    { value: 'all', label: 'Allt' },
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

    if (type === 'transfer') {
        return 'Överföring'
    }

    return 'Ränta'
}

function getTransactionBadgeType(transaction: { type: TransactionType; amount: number }): TransactionType {
    if (transaction.type === 'transfer') {
        return transaction.amount < 0 ? 'withdrawal' : 'deposit'
    }

    return transaction.type
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

    if (transactionType === 'TRANSFER_OUT' || transactionType === 'TRANSFER_IN') {
        return 'transfer'
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

// API:t skickar UTC ("…Z"), webbläsaren visar svensk tid
function formatTransactionTime(date: string) {
    return new Intl.DateTimeFormat('sv-SE', {
        hour: '2-digit',
        minute: '2-digit',
    }).format(new Date(date))
}

type TransactionStatus = 'pending' | 'failed'

// Genomförda transaktioner får ingen etikett, bara de som inte är klara
function mapBackendTransactionStatus(status: string): TransactionStatus | null {
    if (status === 'FAILED') {
        return 'failed'
    }

    if (status === 'PENDING' || status === 'PROCESSING') {
        return 'pending'
    }

    return null
}

const transactionStatusLabels: Record<TransactionStatus, string> = {
    pending: 'Pågår',
    failed: 'Misslyckades',
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

    if (type === 'transfer') {
        return 'Överföring'
    }

    return 'Ränta'
}

function mapBackendTransactionToHistoryTransaction(
    transaction: BackendTransaction,
    account: BackendAccount
) {
    const type = mapBackendTransactionType(transaction.transactionType)
    const rawAmount = parseBackendAmount(transaction.amount)
    const amount =
        type === 'withdrawal' || transaction.transactionType === 'TRANSFER_OUT'
            ? -Math.abs(rawAmount)
            : Math.abs(rawAmount)
    const date = transaction.completedAt ?? transaction.createdAt

    return {
        id: transaction.id,
        title: getTransactionTitle(type),
        accountName: account.name,
        counterparty: transaction.counterparty,
        date: `${formatTransactionDate(date)} · ${formatTransactionTime(date)}`,
        rawDate: date,
        month: formatTransactionMonth(date),
        status: mapBackendTransactionStatus(transaction.status),
        amount,
        type,
    }
}

function HistoryPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()
    const [searchParams, setSearchParams] = useSearchParams()
    const [activeFilter, setActiveFilter] = useState<HistoryFilter>('all')
    const [activePeriodFilter, setActivePeriodFilter] = useState<PeriodFilter>('all')
    const [currentPage, setCurrentPage] = useState(1)
    const pageSize = 20
    const accountParam = searchParams.get('konto')?.trim()
    const activeAccount: AccountFilter = accountParam || 'all'
    const { data: accounts = [], isLoading: isLoadingAccounts, isError: accountsError } = useQuery({
        queryKey: ['accounts'],
        queryFn: getAccounts,
        staleTime: 30_000,
    })
    const activeAccountId = accounts.find((account) => account.name === activeAccount)?.id
    const backendTransactionType =
        activeFilter === 'deposit'
            ? 'DEPOSIT'
            : activeFilter === 'withdrawal'
                ? 'WITHDRAWAL'
                : activeFilter === 'interest'
                    ? 'INTEREST'
                    : undefined
    const {
        data: transactionHistory,
        isLoading: isLoadingTransactions,
        isError: transactionsError,
    } = useQuery({
        queryKey: ['transactions', currentPage, pageSize, activeAccountId ?? 'all', backendTransactionType ?? 'all'],
        queryFn: () =>
            getTransactions(currentPage, pageSize, {
                accountId: activeAccountId,
                type: backendTransactionType,
            }),
        enabled: activeAccount === 'all' || Boolean(activeAccountId),
        staleTime: 30_000,
    })
    const accountById = new Map(accounts.map((account) => [account.id, account]))
    const fallbackAccount: BackendAccount = {
        id: '',
        accountNumber: '',
        accountType: '',
        name: 'Okänt konto',
        status: '',
        balance: '0',
    }
    const transactions = (transactionHistory?.transactions ?? []).map((transaction) =>
        mapBackendTransactionToHistoryTransaction(
            transaction,
            accountById.get(transaction.accountId ?? '') ?? fallbackAccount
        )
    )

    const accountFilters = accounts.map((account) => account.name)
    const totalPages = transactionHistory?.totalPages ?? 1

    const visibleTransactions = transactions.filter((transaction) => {
        const matchesType = activeFilter === 'all' || transaction.type === activeFilter
        const transactionDate = new Date(transaction.rawDate)
        const now = new Date()
        const thirtyDaysAgo = new Date(now)

        thirtyDaysAgo.setDate(now.getDate() - 30)

        // Periodfiltret gäller bara den redan hämtade sidan, eftersom backend paginerar per konto.
        const matchesPeriod =
            activePeriodFilter === 'all' ||
            (activePeriodFilter === 'last30days' && transactionDate >= thirtyDaysAgo) ||
            (activePeriodFilter === 'thisYear' && transactionDate.getFullYear() === now.getFullYear())

        return matchesType && matchesPeriod
    })

    function handleAccountChange(accountName: AccountFilter) {
        const nextSearchParams = new URLSearchParams(searchParams)

        if (accountName === 'all') {
            nextSearchParams.delete('konto')
        } else {
            nextSearchParams.set('konto', accountName)
        }

        setSearchParams(nextSearchParams)
        setCurrentPage(1)
    }

    function handleTypeChange(filter: HistoryFilter) {
        setActiveFilter(filter)
        setCurrentPage(1)
    }

    function handlePeriodChange(filter: PeriodFilter) {
        setActivePeriodFilter(filter)
        setCurrentPage(1)
    }

    function goToPreviousPage() {
        setCurrentPage((page) => Math.max(1, page - 1))
    }

    function goToNextPage() {
        setCurrentPage((page) => Math.min(totalPages, page + 1))
    }

    const selectedAccountLabel = activeAccount === 'all' ? 'Alla konton' : activeAccount
    const isLoadingHistory = isLoadingAccounts || isLoadingTransactions
    const hasHistoryError = accountsError || transactionsError

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
                        <h1>Transaktionshistorik</h1>
                        <p>Alla rörelser på dina sparkonton.</p>
                    </header>

                    <div className="history-controls">
                        <div className="history-controls-desktop">
                            <label
                                className={`history-select-filter history-select-filter--account ${
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
                                className={`history-select-filter history-select-filter--type ${
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

                            <label
                                className={`history-select-filter history-select-filter--period ${
                                    activePeriodFilter !== 'all' ? 'history-select-filter--active' : ''
                                }`}
                            >
                                <span>Period</span>
                                <select
                                    aria-label="Välj period"
                                    value={activePeriodFilter}
                                    onChange={(event) => handlePeriodChange(event.target.value as PeriodFilter)}
                                >
                                    {periodFilters.map((filter) => (
                                        <option value={filter.value} key={filter.value}>
                                            {filter.label}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>

                        <div className="history-controls-mobile">
                            <label
                                className={`history-select-filter history-select-filter--account ${
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
                                className={`history-select-filter history-select-filter--type ${
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

                            <label
                                className={`history-select-filter history-select-filter--period ${
                                    activePeriodFilter !== 'all' ? 'history-select-filter--active' : ''
                                }`}
                            >
                                <span>Period</span>
                                <select
                                    aria-label="Välj period"
                                    value={activePeriodFilter}
                                    onChange={(event) => handlePeriodChange(event.target.value as PeriodFilter)}
                                >
                                    {periodFilters.map((filter) => (
                                        <option value={filter.value} key={filter.value}>
                                            {filter.label}
                                        </option>
                                    ))}
                                </select>
                            </label>
                        </div>
                    </div>

                    {isLoadingHistory ? (
                        <div className="history-empty">
                            <h2>Laddar transaktioner</h2>
                            <p>Hämtar historik för dina konton.</p>
                        </div>
                    ) : hasHistoryError ? (
                        <div className="history-empty">
                            <h2>Historiken kunde inte hämtas</h2>
                            <p>Försök igen om en stund.</p>
                        </div>
                    ) : visibleTransactions.length > 0 ? (
                        <div className="history-list">
                            {Object.entries(groupedTransactions).map(([month, transactions]) => (
                                <section className="history-month" key={month}>
                                    <h2>{month}</h2>

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
                                                <h3>{transaction.title}</h3>
                                                <p>
                                                    {transaction.accountName}
                                                    {transaction.counterparty && ` · ${transaction.counterparty}`} ·{' '}
                                                    <span className="transaction-date">{transaction.date}</span>
                                                    {transaction.status && (
                                                        <span
                                                            className={`transaction-status transaction-status--${transaction.status}`}
                                                        >
                                                            {transactionStatusLabels[transaction.status]}
                                                        </span>
                                                    )}
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

                                                <span
                                                    className={`transaction-badge transaction-badge--${getTransactionBadgeType(
                                                        transaction
                                                    )}`}
                                                >
                                                    {getTransactionLabel(getTransactionBadgeType(transaction))}
                                                </span>
                                            </div>
                                        </article>
                                    ))}
                                </section>
                            ))}
                        </div>
                    ) : (
                        <div className="history-empty">
                            <h2>Inga transaktioner hittades</h2>
                            <p>Det finns inga rörelser för {selectedAccountLabel} som matchar det valda filtret.</p>
                        </div>
                    )}

                    {!isLoadingHistory && !hasHistoryError && totalPages > 1 && (
                        <nav className="history-pagination" aria-label="Sidnavigering för transaktionshistorik">
                            <button
                                type="button"
                                className="history-pagination__button"
                                onClick={goToPreviousPage}
                                disabled={currentPage === 1}
                                aria-label="Visa föregående sida"
                            >
                                <ChevronLeft size={16} />
                            </button>
                            <span className="history-pagination__status">
                                Sida {currentPage} av {totalPages}
                            </span>
                            <button
                                type="button"
                                className="history-pagination__button"
                                onClick={goToNextPage}
                                disabled={currentPage === totalPages}
                                aria-label="Visa nästa sida"
                            >
                                <ChevronRight size={16} />
                            </button>
                        </nav>
                    )}

                </section>

                <CustomerServiceFooter />
            </main>
        </div>
    )
}

export default HistoryPage
