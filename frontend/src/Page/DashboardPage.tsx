import { type DashboardAccountTransaction } from '../components/DashboardActions/mockDashboardAccountTransactions'
import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { UserProfile } from '../components/UserProfile'
import { useModalFromLink } from '../hooks/useModalFromLink'
import './DashboardPage.css'
import { AppNav } from '../components/AppNav'
import {
    createAccount,
    deposit,
    renameAccount,
    withdraw,
    type BackendAccount,
    type BackendTransaction,
} from '../services/accountService'
import { useAccountsWithTransactions } from '../hooks/useAccountsWithTransactions'
import { useQueryClient } from '@tanstack/react-query'
import { type AccountWithTransactions } from '../services/accountOverviewService'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { BalanceOverview } from '../components/DashboardActions/BalanceOverview'
import { DashboardActions } from '../components/DashboardActions/DashboardActions'
import { RecentEvents } from '../components/RecentEvents/RecentEvents'
import { NextEvent } from '../components/NextEvent/NextEvent'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import type { Account } from '../components/DashboardActions/BalanceOverview'
import type { DashboardAction } from '../components/DashboardActions/mockDashboardActions'
import { useLogout } from '../hooks/useLogout'
import { useTheme } from '../context/useTheme'
import { Modal } from '../components/Modal'
import { Input } from '../components/Input'
import { Button } from '../components/Button'
import { ArrowRight, Plus } from 'lucide-react'
import {
    accountIconOptions,
    accountVariantOptions,
    getAccountIcon,
    getAccountPresentation,
    setAccountPresentation,
    type AccountIconId,
    type AccountPresentation,
    type AccountVariant,
} from '../utils/accountPresentation'
import { formatAccountInterest, getAccountInterest } from '../utils/accountInterest'
import { getNextEvent } from '../utils/nextEvent'
import { getTransactionsNewestFirst } from '../utils/transactionOrder'
import { OrderReceipt, type OrderReceiptData } from '../components/OrderReceipt/OrderReceipt'
import { createAccountSchema } from '../schemas/accountSchema'
import { transactionAmountSchema } from '../schemas/transactionSchema'

type CreateAccountIconId = AccountIconId
type CreateAccountVariant = AccountVariant

function parseKr(value: string) {
    return Number(value.replace(/\s/g, '').replace('kr', '').replace(',', '.'))
}

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE')} kr`
}

function parseBackendBalance(balance: string) {
    return Number(balance.replace(/\s/g, '').replace(',', '.'))
}

function formatBackendBalance(balance: string) {
    const numericBalance = parseBackendBalance(balance)

    if (Number.isNaN(numericBalance)) {
        return '0 kr'
    }

    return formatKr(numericBalance)
}

function mapBackendAccountToDashboardAccount(account: BackendAccount, index: number): Account {
    const presentation = getAccountPresentation(account.id, index)
    const interest = getAccountInterest(account)

    return {
        id: account.id,
        icon: getAccountIcon(presentation.iconId),
        label: account.name.toUpperCase(),
        value: formatBackendBalance(account.balance),
        interest: interest ? formatAccountInterest(interest) : undefined,
        interestType: interest?.type,
        accountNumber: account.accountNumber,
        variant: presentation.variant,
    }
}

function mapBackendTransactionType(transactionType: string): DashboardAccountTransaction['type'] {
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

function formatDashboardTransactionDate(date: string) {
    return new Intl.DateTimeFormat('sv-SE', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
    }).format(new Date(date))
}

function mapBackendTransactionToDashboardTransaction(
    transaction: BackendTransaction,
    accountId: string
): DashboardAccountTransaction {
    const type = mapBackendTransactionType(transaction.transactionType)
    const rawAmount = parseBackendBalance(transaction.amount)
    const amount =
        type === 'withdrawal' || transaction.transactionType === 'TRANSFER_OUT'
            ? -Math.abs(rawAmount)
            : Math.abs(rawAmount)
    const date = transaction.completedAt ?? transaction.createdAt

    return {
        id: transaction.id,
        accountId,
        title: type === 'deposit'
            ? 'Insättning'
            : type === 'withdrawal'
                ? 'Uttag'
                : type === 'transfer'
                    ? 'Överföring'
                    : 'Ränta',
        date: formatDashboardTransactionDate(date),
        amount,
        type,
        counterparty: transaction.counterparty,
    }
}

function formatAccountName(value: string) {
    return value.toLocaleLowerCase('sv-SE').replace(/(^|\s)\S/g, (letter) => letter.toLocaleUpperCase('sv-SE'))
}

function DashboardPage() {
    const navigate = useNavigate()
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()
    const queryClient = useQueryClient()

    const { data: accountTransactionGroups, isLoading: isLoadingAccounts, isError: accountsError } = useAccountsWithTransactions()

    const accounts = (accountTransactionGroups ?? []).map(({ account }, index) =>
        mapBackendAccountToDashboardAccount(account, index)
    )
    const nextEvent = getNextEvent(accountTransactionGroups ?? [])
    const accountTransactions = getTransactionsNewestFirst(accountTransactionGroups ?? []).map(({ account, transaction }) =>
        mapBackendTransactionToDashboardTransaction(transaction, account.id)
    )

    const [createAccountError, setCreateAccountError] = useState('')
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
    // FAQ-svaret "Skapa nytt sparkonto" länkar hit och öppnar modalen direkt
    const createAccountLink = useModalFromLink('createAccount')
    const [newAccountName, setNewAccountName] = useState('')
    const [newAccountIconId, setNewAccountIconId] = useState<CreateAccountIconId>('piggyBank')
    const [newAccountVariant, setNewAccountVariant] = useState<CreateAccountVariant>('default')
    const [selectedAccountId, setSelectedAccountId] = useState<string | null>(null)
    const [modalTransactionType, setModalTransactionType] = useState<'deposit' | 'withdrawal'>('deposit')
    const [modalAmount, setModalAmount] = useState('')
    const [modalTransactionMessage, setModalTransactionMessage] = useState('')
    const [modalReceipt, setModalReceipt] = useState<OrderReceiptData | null>(null)
    const [dashboardTransactionType, setDashboardTransactionType] = useState<'deposit' | 'withdrawal' | null>(null)
    const [dashboardTransactionAccountId, setDashboardTransactionAccountId] = useState('')
    const [dashboardTransactionAmount, setDashboardTransactionAmount] = useState('')
    const [dashboardTransactionMessage, setDashboardTransactionMessage] = useState('')
    const [dashboardReceipt, setDashboardReceipt] = useState<OrderReceiptData | null>(null)

    const selectedAccount = accounts.find((account) => account.id === selectedAccountId) ?? null
    const selectedBackendAccountName =
        accountTransactionGroups?.find(({ account }) => account.id === selectedAccountId)?.account.name ?? null

    const dashboardTransactionAccount =
        accounts.find((account) => account.id === dashboardTransactionAccountId) ?? accounts[0] ?? null
    const dashboardTransactionTitle = dashboardTransactionType === 'deposit' ? 'Sätt in pengar' : 'Ta ut pengar'
    const dashboardTransactionButtonLabel = dashboardTransactionType === 'deposit' ? 'Sätt in' : 'Ta ut'

    const selectedAccountTransactions = selectedAccount
        ? accountTransactions.filter((transaction) => transaction.accountId === selectedAccount.id)
        : []

    const selectedAccountEvents = selectedAccountTransactions.map((transaction) => ({
        id: transaction.id,
        title: transaction.title,
        accountName: selectedAccount ? formatAccountName(selectedAccount.label) : '',
        date: transaction.date,
        amount: transaction.amount,
        type: transaction.type,
        counterparty: transaction.counterparty,
    }))
    const selectedAccountRecentEvents = selectedAccountEvents.slice(0, 5)
    const selectedAccountHistoryUrl = selectedBackendAccountName
        ? `/historik?konto=${encodeURIComponent(selectedBackendAccountName)}`
        : '/historik'

    const recentDashboardEvents = accountTransactions
        .map((transaction) => {
            const account = accounts.find((item) => item.id === transaction.accountId)

            return {
                id: transaction.id,
                title: transaction.title,
                accountName: account ? formatAccountName(account.label) : '',
                date: transaction.date,
                amount: transaction.amount,
                type: transaction.type,
                counterparty: transaction.counterparty,
            }
        })
        .slice(0, 5)

    function handleAccountClick(accountId: string) {
        setSelectedAccountId(accountId)
        setModalTransactionMessage('')
        setModalReceipt(null)
    }

    function handleModalTransactionTypeChange(type: 'deposit' | 'withdrawal') {
        setModalTransactionType(type)
        setModalTransactionMessage('')
        setModalReceipt(null)
    }

    function openDashboardTransactionModal(type: 'deposit' | 'withdrawal') {
        setDashboardTransactionType(type)
        setDashboardTransactionAccountId((currentAccountId) => currentAccountId || accounts[0]?.id || '')
        setDashboardTransactionAmount('')
        setDashboardTransactionMessage('')
        setDashboardReceipt(null)
    }

    function closeDashboardTransactionModal() {
        setDashboardTransactionType(null)
        setDashboardTransactionAmount('')
        setDashboardTransactionMessage('')
        setDashboardReceipt(null)
    }

    function handleActionClick(action: DashboardAction['action']) {
        switch (action) {
            case 'deposit':
                openDashboardTransactionModal('deposit')
                break
            case 'withdraw':
                openDashboardTransactionModal('withdrawal')
                break
            case 'history':
                navigate('/historik')
                break
            case 'tax':
                navigate('/skatt')
                break
        }
    }

    async function handleCreateAccount() {
        const validation = createAccountSchema.safeParse({
            name: newAccountName,
        })

        if (!validation.success) {
            setCreateAccountError(validation.error.issues[0]?.message ?? 'Kontrollera kontonamnet.')
            return
        }

        const trimmedName = validation.data.name

        const presentation: AccountPresentation = {
            iconId: newAccountIconId,
            variant: newAccountVariant,
        }

        try {
            const createdAccount = await createAccount('SAVINGS')
            const accountWithName = await renameAccount(createdAccount.id, trimmedName)

            setAccountPresentation(accountWithName.id, presentation)
            queryClient.setQueryData<AccountWithTransactions[]>(['accountsWithTransactions', 1, 20], (prev) => [
                ...(prev ?? []),
                {
                    account: accountWithName,
                    history: {
                        transactions: [],
                        page: 1,
                        pageSize: 20,
                        totalCount: 0,
                        totalPages: 0,
                    },
                },
            ])
            setNewAccountName('')
            setNewAccountIconId('piggyBank')
            setNewAccountVariant('default')
            setCreateAccountError('')
            setIsCreateModalOpen(false)
            createAccountLink.clearRequest()
        } catch (error) {
            setCreateAccountError(error instanceof Error ? error.message : 'Kunde inte skapa konto.')
        }
    }

    async function handleModalTransaction() {
        if (!selectedAccount) return

        const validation = transactionAmountSchema.safeParse({
            amount: modalAmount,
        })

        if (!validation.success) {
            setModalTransactionMessage(validation.error.issues[0]?.message ?? 'Kontrollera beloppet.')
            return
        }

        const amount = Number(validation.data.amount.replace(',', '.'))
        const currentBalance = parseKr(selectedAccount.value)

        if (modalTransactionType === 'withdrawal' && amount > currentBalance) {
            setModalTransactionMessage('Beloppet överstiger tillgängligt saldo.')
            return
        }

        try {
            const result =
                modalTransactionType === 'deposit'
                    ? await deposit(selectedAccount.id, amount)
                    : await withdraw(selectedAccount.id, amount)

            queryClient.setQueryData<AccountWithTransactions[]>(['accountsWithTransactions', 1, 20], (prev) =>
                (prev ?? []).map((group) =>
                    group.account.id === selectedAccount.id
                        ? {
                              account: { ...group.account, balance: result.balance },
                              history: {
                                  ...group.history,
                                  transactions: [
                                      {
                                          id: result.transactionId,
                                          transactionType: modalTransactionType === 'deposit' ? 'DEPOSIT' : 'WITHDRAWAL',
                                          amount: amount.toFixed(2),
                                          status: 'COMPLETED',
                                          createdAt: new Date().toISOString(),
                                          completedAt: new Date().toISOString(),
                                      },
                                      ...group.history.transactions,
                                  ],
                                  totalCount: group.history.totalCount + 1,
                                  totalPages: Math.ceil((group.history.totalCount + 1) / group.history.pageSize),
                              },
                          }
                        : group
                )
            )
            setModalTransactionMessage('')
            setModalReceipt({
                type: modalTransactionType,
                amount,
                accountName: formatAccountName(selectedAccount.label),
                balance: parseBackendBalance(result.balance),
            })
            setModalAmount('')
            await queryClient.invalidateQueries({ queryKey: ['accountsWithTransactions'] })
        } catch (error) {
            setModalTransactionMessage(error instanceof Error ? error.message : 'Transaktionen misslyckades.')
        }
    }

    async function handleDashboardTransaction() {
        if (!dashboardTransactionType || !dashboardTransactionAccount) return

        const validation = transactionAmountSchema.safeParse({
            amount: dashboardTransactionAmount,
        })

        if (!validation.success) {
            setDashboardTransactionMessage(validation.error.issues[0]?.message ?? 'Kontrollera beloppet.')
            return
        }

        const amount = Number(validation.data.amount.replace(',', '.'))

        const currentBalance = parseKr(dashboardTransactionAccount.value)

        if (dashboardTransactionType === 'withdrawal' && amount > currentBalance) {
            setDashboardTransactionMessage('Beloppet överstiger tillgängligt saldo.')
            return
        }

        try {
            const result =
                dashboardTransactionType === 'deposit'
                    ? await deposit(dashboardTransactionAccount.id, amount)
                    : await withdraw(dashboardTransactionAccount.id, amount)

            queryClient.setQueryData<AccountWithTransactions[]>(['accountsWithTransactions', 1, 20], (prev) =>
                (prev ?? []).map((group) =>
                    group.account.id === dashboardTransactionAccount.id
                        ? {
                              account: { ...group.account, balance: result.balance },
                              history: {
                                  ...group.history,
                                  transactions: [
                                      {
                                          id: result.transactionId,
                                          transactionType: dashboardTransactionType === 'deposit' ? 'DEPOSIT' : 'WITHDRAWAL',
                                          amount: amount.toFixed(2),
                                          status: 'COMPLETED',
                                          createdAt: new Date().toISOString(),
                                          completedAt: new Date().toISOString(),
                                      },
                                      ...group.history.transactions,
                                  ],
                                  totalCount: group.history.totalCount + 1,
                                  totalPages: Math.ceil((group.history.totalCount + 1) / group.history.pageSize),
                              },
                          }
                        : group
                )
            )
            setDashboardTransactionMessage('')
            setDashboardReceipt({
                type: dashboardTransactionType,
                amount,
                accountName: formatAccountName(dashboardTransactionAccount.label),
                balance: parseBackendBalance(result.balance),
            })
            setDashboardTransactionAmount('')
            await queryClient.invalidateQueries({ queryKey: ['accountsWithTransactions'] })
        } catch (error) {
            setDashboardTransactionMessage(error instanceof Error ? error.message : 'Transaktionen misslyckades.')
        }
    }

    return (
        <div className="dashboard-page">
            <DecorativeCircle color="orange" size={170} left={-40} top={200} />
            <DecorativeCircle color="blue" size={100} left={20} top={310} opacity={0.9} />
            <DecorativeCircle color="orange" size={60} left={280} bottom={60} />
            <DecorativeCircle color="green" size={140} right={-30} bottom={140} />
            <DecorativeCircle color="green" size={90} right={40} bottom={30} opacity={0.9} />

            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <main className="dashboard-main">
                <UserProfile />

                <div className="dashboard-content">
                    <div className="dashboard-brand-card brand-logo">
                        <span>Sparportal</span>
                        <strong>nordiska<span className="dashboard-brand-dot">.</span></strong>
                    </div>

                    {isLoadingAccounts ? (
                        <div className="dashboard-empty-card">
                            <h2>Laddar konton</h2>
                            <p>Hämtar dina konton...</p>
                        </div>
                    ) : accountsError ? (
                        <div className="dashboard-empty-card">
                            <h2>Konton kunde inte hämtas</h2>
                        </div>
                    ) : accounts.length === 0 ? (
                        <div className="dashboard-empty-card">
                            <h2>Du har inga konton än</h2>
                            <p>Skapa ett sparkonto för att komma igång.</p>
                        </div>
                    ) : (
                        <BalanceOverview
                            totalLabel="TOTALT SPARAT"
                            totalValue={formatKr(
                                accounts.reduce((sum, account) => sum + parseKr(account.value), 0)
                            )}
                            subLabel={`${accounts.length} konton`}
                            accounts={accounts}
                            onAccountClick={handleAccountClick}
                        />
                    )}

                    <NextEvent event={nextEvent} />

                    <button
                        type="button"
                        className="create-account-btn"
                        onClick={() => setIsCreateModalOpen(true)}
                    >
                        <Plus size={16} />
                        Skapa nytt sparkonto
                    </button>

                    <Modal
                        isOpen={isCreateModalOpen || createAccountLink.isRequested}
                        onClose={() => {
                            setIsCreateModalOpen(false)
                            createAccountLink.clearRequest()
                            setCreateAccountError('')
                        }}
                        title="Skapa nytt sparkonto"
                    >
                        <div className="create-account-modal">
                            <Input
                                label="Kontonamn"
                                type="text"
                                placeholder="T.ex. Resekassa"
                                value={newAccountName}
                                onChange={(e) => {
                                    setNewAccountName(e.target.value)
                                    setCreateAccountError('')
                                }}
                            />

                            <p className="create-account-modal__message" role={createAccountError ? 'alert' : undefined}>
                                {createAccountError}
                            </p>

                            <div className="create-account-modal__field">
                                <p>Ikon</p>
                                <div className="create-account-modal__option-grid">
                                    {accountIconOptions.map((option) => (
                                        <button
                                            type="button"
                                            key={option.id}
                                            className={`create-account-modal__icon-option ${
                                                newAccountIconId === option.id ? 'create-account-modal__icon-option--active' : ''
                                            }`}
                                            aria-label={option.label}
                                            aria-pressed={newAccountIconId === option.id}
                                            onClick={() => setNewAccountIconId(option.id)}
                                        >
                                            {option.icon}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            <div className="create-account-modal__field">
                                <p>Färg</p>
                                <div className="create-account-modal__color-row">
                                    {accountVariantOptions.map((option) => (
                                        <button
                                            type="button"
                                            key={option.value}
                                            className={`create-account-modal__color-option create-account-modal__color-option--${option.value} ${
                                                newAccountVariant === option.value ? 'create-account-modal__color-option--active' : ''
                                            }`}
                                            aria-pressed={newAccountVariant === option.value}
                                            onClick={() => setNewAccountVariant(option.value)}
                                        >
                                            <span />
                                            {option.label}
                                        </button>
                                    ))}
                                </div>
                            </div>

                            <div className="create-account-modal__actions">
                                <Button type="button" variant="primary" onClick={handleCreateAccount}>
                                    Skapa konto
                                </Button>
                            </div>
                        </div>
                    </Modal>

                    <Modal
                        isOpen={selectedAccount !== null}
                        onClose={() => setSelectedAccountId(null)}
                    >
                        {selectedAccount && (
                            <div className="account-modal">
                                <div
                                    className={`balance-card account-modal__balance-card account-modal__balance-card--${selectedAccount.variant ?? 'default'}`}
                                >
                                    <p
                                        className={`account-modal__account-name account-modal__account-name--${selectedAccount.variant ?? 'default'}`}
                                    >
                                        {selectedAccount.icon}
                                        <span>{selectedAccount.label}</span>
                                    </p>
                                    <p className="balance-sub">Totalt saldo</p>
                                    <p
                                        className={`balance-value account-modal__balance-value account-modal__balance-value--${selectedAccount.variant ?? 'default'}`}
                                    >
                                        {selectedAccount.value}
                                    </p>

                                    {(selectedAccount.interest || selectedAccount.accountNumber) && (
                                        <dl className="account-modal__details">
                                            {selectedAccount.interest && (
                                                <div>
                                                    <dt>Ränta</dt>
                                                    <dd>{selectedAccount.interest}</dd>
                                                </div>
                                            )}
                                            {selectedAccount.accountNumber && (
                                                <div>
                                                    <dt>Kontonummer</dt>
                                                    <dd>{selectedAccount.accountNumber}</dd>
                                                </div>
                                            )}
                                        </dl>
                                    )}
                                </div>

                                <div className="account-modal__history">
                                    {selectedAccountEvents.length > 0 ? (
                                        <RecentEvents events={selectedAccountRecentEvents} title="De 5 senaste händelserna" />
                                    ) : (
                                        <p className="account-modal__empty">Inga händelser hittades för kontot.</p>
                                    )}

                                    <Link className="account-modal__history-link" to={selectedAccountHistoryUrl}>
                                        <span>Visa fler händelser</span>
                                        <ArrowRight size={16} />
                                    </Link>
                                </div>

                                <div className="account-modal__section account-modal__transfer">
                                    <h3>Sätt in eller ta ut</h3>

                                    <div className="pill-toggle-row account-modal__mode-row">
                                        <button
                                            type="button"
                                            className={`pill-toggle pill-toggle--deposit ${modalTransactionType === 'deposit' ? 'pill-toggle--active' : ''}`}
                                            onClick={() => handleModalTransactionTypeChange('deposit')}
                                        >
                                            Sätt in
                                        </button>

                                        <button
                                            type="button"
                                            className={`pill-toggle pill-toggle--withdraw ${modalTransactionType === 'withdrawal' ? 'pill-toggle--active' : ''}`}
                                            onClick={() => handleModalTransactionTypeChange('withdrawal')}
                                        >
                                            Ta ut
                                        </button>
                                    </div>

                                    <div className="transact-form-card account-modal__transfer-card">
                                        <div className="transact-field">
                                            <label className="transact-label" htmlFor="account-modal-amount">
                                                Belopp (kr)
                                            </label>
                                            <input
                                                id="account-modal-amount"
                                                className="transact-pill-input"
                                                type="text"
                                                placeholder="0"
                                                value={modalAmount}
                                                onChange={(e) => {
                                                    setModalAmount(e.target.value)
                                                    setModalTransactionMessage('')
                                                    setModalReceipt(null)
                                                }}
                                            />
                                            <p
                                                className={`dashboard-available-balance ${
                                                    modalTransactionType === 'deposit'
                                                        ? 'dashboard-available-balance--empty'
                                                        : ''
                                                }`}
                                            >
                                                Tillgängligt: {selectedAccount.value}
                                            </p>
                                        </div>

                                        <p
                                            className={`transact-message account-modal__message ${
                                                modalTransactionMessage || modalReceipt ? '' : 'account-modal__message--empty'
                                            } ${modalTransactionMessage ? 'account-modal__message--error' : ''}`}
                                            role={modalTransactionMessage ? 'alert' : modalReceipt ? 'status' : undefined}
                                        >
                                            {modalTransactionMessage ||
                                                (modalReceipt && <OrderReceipt receipt={modalReceipt} />)}
                                        </p>

                                        <button
                                            type="button"
                                            className={`transact-submit-btn ${
                                                modalTransactionType === 'deposit'
                                                    ? 'transact-submit-btn--deposit'
                                                    : 'transact-submit-btn--withdraw'
                                            }`}
                                            onClick={handleModalTransaction}
                                        >
                                            {modalTransactionType === 'deposit' ? 'Sätt in pengar' : 'Ta ut pengar'}
                                        </button>
                                    </div>
                                </div>
                            </div>
                        )}
                    </Modal>
                    <Modal
                        isOpen={dashboardTransactionType !== null}
                        onClose={closeDashboardTransactionModal}
                        title={dashboardTransactionTitle}
                    >
                        {dashboardTransactionAccount && (
                            <div className="dashboard-transaction-modal">
                                <label className="dashboard-transaction-modal__field">
                                    <span>Konto</span>
                                    <select
                                        value={dashboardTransactionAccount.id}
                                        onChange={(e) => {
                                            setDashboardTransactionAccountId(e.target.value)
                                            setDashboardTransactionMessage('')
                                            setDashboardReceipt(null)
                                        }}
                                    >
                                        {accounts.map((account) => (
                                            <option key={account.id} value={account.id}>
                                                {formatAccountName(account.label)} - {account.value}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <Input
                                    label="Belopp"
                                    type="amount"
                                    placeholder="0"
                                    value={dashboardTransactionAmount}
                                    onChange={(e) => {
                                        setDashboardTransactionAmount(e.target.value)
                                        setDashboardTransactionMessage('')
                                        setDashboardReceipt(null)
                                    }}
                                />
                                <p
                                    className={`dashboard-transaction-modal__message ${
                                        dashboardTransactionType === 'deposit' && !dashboardTransactionMessage && !dashboardReceipt
                                            ? 'dashboard-transaction-modal__message--empty'
                                            : ''
                                    }`}
                                    role={dashboardTransactionMessage ? 'alert' : dashboardReceipt ? 'status' : undefined}
                                >
                                    {dashboardTransactionMessage ||
                                        (dashboardReceipt && <OrderReceipt receipt={dashboardReceipt} />) ||
                                        `Tillgängligt: ${dashboardTransactionAccount.value}`}
                                </p>

                                <div className="dashboard-transaction-modal__actions">
                                    <Button type="button" variant="secondary" onClick={closeDashboardTransactionModal}>
                                        Avbryt
                                    </Button>
                                    <Button type="button" variant="primary" onClick={handleDashboardTransaction}>
                                        {dashboardTransactionButtonLabel}
                                    </Button>
                                </div>
                            </div>
                        )}
                    </Modal>
                    <RecentEvents events={recentDashboardEvents} />

                    <DashboardActions onActionClick={handleActionClick} />
                </div>

                <CustomerServiceFooter />
            </main>
        </div>
    )
}

export default DashboardPage
