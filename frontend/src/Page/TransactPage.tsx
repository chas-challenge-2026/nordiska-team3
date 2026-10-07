import { z } from 'zod'
import { type FormEvent, useEffect, useState } from 'react'
import { useQueryClient } from '@tanstack/react-query'
import { UserProfile } from '../components/UserProfile'
import { useLogout } from '../hooks/useLogout'
import './TransactPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { type TransactAccount } from './mockTransactAccounts'
import { deposit, getAccounts, withdraw, type BackendAccount } from '../services/accountService'
import { ChevronDown } from 'lucide-react'
import { useTheme } from '../context/useTheme'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { customerServiceContact } from '../content/customerServiceContact'
import { OrderReceipt, type OrderReceiptData } from '../components/OrderReceipt/OrderReceipt'
import {
    getAccountIcon,
    getAccountPresentation,
    type AccountPresentation,
} from '../utils/accountPresentation'
import { amountSchema, transactSchema } from '../schemas/transactionSchema'

const transferSchema = z
    .object({
        accountId: z.string().trim().min(1, 'Välj ett konto att flytta från.'),
        toAccountId: z.string().trim().min(1, 'Välj ett konto att flytta till.'),
        amount: amountSchema,
    })
    .refine((data) => data.accountId !== data.toAccountId, {
        message: 'Från- och till-konto måste vara olika.',
        path: ['toAccountId'],
    })

type Mode = 'deposit' | 'withdraw' | 'transfer'

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE')} kr`
}

function parseBackendBalance(balance: string) {
    return Number(balance.replace(/\s/g, '').replace(',', '.'))
}

function mapBackendAccountToTransactAccount(
    account: BackendAccount,
    presentation: AccountPresentation
): TransactAccount {
    return {
        id: account.id,
        name: account.name,
        balance: parseBackendBalance(account.balance),
        icon: getAccountIcon(presentation.iconId),
        variant: presentation.variant,
    }
}

function TransactPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()
    const queryClient = useQueryClient()

    const [mode, setMode] = useState<Mode>('deposit')
    const [accounts, setAccounts] = useState<TransactAccount[]>([])
    const [accountId, setAccountId] = useState('')
    const [toAccountId, setToAccountId] = useState('')
    const [isLoadingAccounts, setIsLoadingAccounts] = useState(true)
    const [accountsError, setAccountsError] = useState('')
    const [amount, setAmount] = useState('')
    const [error, setError] = useState('')
    const [receipt, setReceipt] = useState<OrderReceiptData | null>(null)
    const [isSubmitting, setIsSubmitting] = useState(false)

    const transferToAccounts = accounts.filter((acc) => acc.id !== accountId)
    const selectedAccount = accounts.find((acc) => acc.id === accountId) ?? accounts[0] ?? null
    const selectedToAccount = transferToAccounts.find((acc) => acc.id === toAccountId) ?? null

    useEffect(() => {
        let isMounted = true

        async function loadAccounts() {
            try {
                setIsLoadingAccounts(true)
                setAccountsError('')

                const backendAccounts = await getAccounts()

                if (!isMounted) return

                const mappedAccounts = backendAccounts.map((account, index) =>
                    mapBackendAccountToTransactAccount(account, getAccountPresentation(account.id, index))
                )

                setAccounts(mappedAccounts)
                setAccountId(mappedAccounts[0]?.id ?? '')
                setToAccountId(mappedAccounts[1]?.id ?? mappedAccounts[0]?.id ?? '')
            } catch {
                if (!isMounted) return

                setAccountsError('Kunde inte hämta konton.')
            } finally {
                if (isMounted) {
                    setIsLoadingAccounts(false)
                }
            }
        }

        loadAccounts()

        return () => {
            isMounted = false
        }
    }, [])

    function handleModeChange(newMode: Mode) {
        if (newMode === 'transfer' && (toAccountId === accountId || !transferToAccounts.some((account) => account.id === toAccountId))) {
            setToAccountId(transferToAccounts[0]?.id ?? '')
        }

        setMode(newMode)
        setError('')
        setReceipt(null)
    }

    function handleFromAccountChange(nextAccountId: string) {
        const nextToAccounts = accounts.filter((account) => account.id !== nextAccountId)

        setAccountId(nextAccountId)

        if (toAccountId === nextAccountId || !nextToAccounts.some((account) => account.id === toAccountId)) {
            setToAccountId(nextToAccounts[0]?.id ?? '')
        }
    }

    async function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setReceipt(null)

        if (mode === 'transfer') {
            const validation = transferSchema.safeParse({
                accountId,
                toAccountId,
                amount,
            })

            if (!validation.success) {
                setError(validation.error.issues[0]?.message ?? 'Kontrollera överföringen.')
                return
            }

            if (!selectedAccount || !selectedToAccount) {
                setError('Inget konto är valt.')
                return
            }

            const numericAmount = Number(validation.data.amount.replace(',', '.'))

            if (numericAmount > selectedAccount.balance) {
                setError('Beloppet överstiger tillgängligt saldo.')
                return
            }

            setError('')
            setIsSubmitting(true)

            // Backend saknar en endpoint för överföring, så den görs som uttag + insättning.
            // Misslyckas insättningen sätts pengarna tillbaka på från-kontot.
            // Knappen är låst (isSubmitting) under alla steg, även återställningen.
            function updateBalance(accountId: string, balance: string) {
                setAccounts((currentAccounts) =>
                    currentAccounts.map((account) =>
                        account.id === accountId ? { ...account, balance: parseBackendBalance(balance) } : account
                    )
                )
            }

            let withdrawResult: Awaited<ReturnType<typeof withdraw>>

            try {
                withdrawResult = await withdraw(selectedAccount.id, numericAmount)
            } catch {
                setError('Överföringen kunde inte genomföras. Inga pengar har flyttats.')
                setIsSubmitting(false)
                return
            }

            try {
                const depositResult = await deposit(selectedToAccount.id, numericAmount)

                updateBalance(selectedAccount.id, withdrawResult.balance)
                updateBalance(selectedToAccount.id, depositResult.balance)
                setReceipt({
                    type: 'transfer',
                    amount: numericAmount,
                    accountName: selectedAccount.name,
                    toAccountName: selectedToAccount.name,
                    balance: parseBackendBalance(withdrawResult.balance),
                })
                setAmount('')
                await queryClient.invalidateQueries({ queryKey: ['accountsWithTransactions'] })
            } catch {
                try {
                    const restoreResult = await deposit(selectedAccount.id, numericAmount)

                    updateBalance(selectedAccount.id, restoreResult.balance)
                    setError(
                        `Överföringen kunde inte genomföras. ${formatKr(numericAmount)} är tillbaka på ${selectedAccount.name}.`
                    )
                } catch {
                    // Pengarna är uttagna men kunde inte sättas tillbaka: saldot ska visa det
                    updateBalance(selectedAccount.id, withdrawResult.balance)
                    setError(
                        `${formatKr(numericAmount)} kunde inte sättas tillbaka. Ring kundservice: ${customerServiceContact.phone}.`
                    )
                }
            } finally {
                setIsSubmitting(false)
            }

            return
        }

        const validation = transactSchema.safeParse({
            accountId,
            amount,
        })

        if (!validation.success) {
            setError(validation.error.issues[0]?.message ?? 'Kontrollera transaktionen.')
            return
        }

        if (!selectedAccount) {
            setError('Inget konto är valt.')
            return
        }

        const numericAmount = Number(validation.data.amount.replace(',', '.'))

        if (mode === 'withdraw' && numericAmount > selectedAccount.balance) {
            setError('Beloppet överstiger tillgängligt saldo.')
            return
        }

        setError('')
        setIsSubmitting(true)

        try {
            const result =
                mode === 'deposit'
                    ? await deposit(selectedAccount.id, numericAmount)
                    : await withdraw(selectedAccount.id, numericAmount)

            const updatedBalance = parseBackendBalance(result.balance)

            setAccounts((currentAccounts) =>
                currentAccounts.map((account) =>
                    account.id === selectedAccount.id
                        ? { ...account, balance: updatedBalance }
                        : account
                )
            )

            setReceipt({
                type: mode === 'deposit' ? 'deposit' : 'withdrawal',
                amount: numericAmount,
                accountName: selectedAccount.name,
                balance: updatedBalance,
            })
            setAmount('')
            await queryClient.invalidateQueries({ queryKey: ['accountsWithTransactions'] })
        } catch (error) {
            setError(error instanceof Error ? error.message : 'Transaktionen misslyckades.')
        } finally {
            setIsSubmitting(false)
        }
    }

    return (
    <div className="dashboard-page">
        <DecorativeCircle color="orange" size={80} left={-20} top={170} />
        <DecorativeCircle color="blue" size={140} left={30} bottom={150} />
        <DecorativeCircle color="orange" size={170} left={180} bottom={40} />
        <DecorativeCircle color="green" size={120} right={90} top={110} opacity={0.9} />
        <DecorativeCircle color="blue" size={190} right={-40} top={40} />

            <div className="transact-brand-circle brand-logo">
        <span>Sparportal</span>
    <strong>nordiska<span className="brand-dot">.</span></strong>
        </div>      

    <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

    <main className="dashboard-main">
        <UserProfile />

        <div className="transact-content">
                <div className="transact-header">
                    <h1 tabIndex={0}>Flytta pengar</h1>
                    <p>Sätt in, ta ut eller flytta medel mellan dina sparkonton.</p>
                </div>

                    <div className="pill-toggle-row">
                        <button
                            type="button"
                            className={`pill-toggle pill-toggle--deposit ${mode === 'deposit' ? 'pill-toggle--active' : ''}`}
                            onClick={() => handleModeChange('deposit')}
                        >
                            Sätt in
                        </button>
                        <button
                            type="button"
                            className={`pill-toggle pill-toggle--withdraw ${mode === 'withdraw' ? 'pill-toggle--active' : ''}`}
                            onClick={() => handleModeChange('withdraw')}
                        >
                            Ta ut
                        </button>
                        <button
                            type="button"
                            className={`pill-toggle pill-toggle--transfer ${mode === 'transfer' ? 'pill-toggle--active' : ''}`}
                            onClick={() => handleModeChange('transfer')}
                        >
                            Mellan konton
                        </button>
                    </div>

                    {isLoadingAccounts ? (
                        <div className="transact-empty-card">
                            <h2 tabIndex={0}>Laddar konton</h2>
                        </div>
                    ) : accountsError ? (
                        <div className="transact-empty-card">
                            <h2 tabIndex={0}>{accountsError}</h2>
                        </div>
                    ) : selectedAccount ? (
                        <div className={`account-preview-grid ${mode === 'transfer' ? '' : 'account-preview-grid--single'}`}>
                            <div className="account-preview-group">
                                <p
                                    className={`account-preview-label ${
                                        mode === 'transfer' ? '' : 'account-preview-label--empty'
                                    }`}
                                >
                                    Från konto
                                </p>
                                <div className={`account-preview-card account-preview-card--${selectedAccount.variant ?? 'default'}`}>
                                    <div className={`account-preview-header account-preview-header--${selectedAccount.variant ?? 'default'}`}>
                                        {selectedAccount.icon}
                                        <span>{selectedAccount.name.toUpperCase()}</span>
                                    </div>
                                    <p className={`account-preview-value account-preview-value--${selectedAccount.variant ?? 'default'}`}>
                                        {formatKr(selectedAccount.balance)}
                                    </p>
                                </div>
                            </div>

                            {mode === 'transfer' && selectedToAccount && (
                                <div className="account-preview-group">
                                    <p className="account-preview-label">Till konto</p>
                                    <div className={`account-preview-card account-preview-card--${selectedToAccount.variant ?? 'default'}`}>
                                        <div className={`account-preview-header account-preview-header--${selectedToAccount.variant ?? 'default'}`}>
                                            {selectedToAccount.icon}
                                            <span>{selectedToAccount.name.toUpperCase()}</span>
                                        </div>
                                        <p className={`account-preview-value account-preview-value--${selectedToAccount.variant ?? 'default'}`}>
                                            {formatKr(selectedToAccount.balance)}
                                        </p>
                                    </div>
                                </div>
                            )}
                        </div>
                    ) : (
                        <div className="transact-empty-card">
                            <h2 tabIndex={0}>Inga konton hittades</h2>
                        </div>
                    )}

                    <form onSubmit={handleSubmit} className="transact-form-card">
                        <div className="transact-field">
                            <label className="transact-label">{mode === 'transfer' ? 'Från konto' : 'Konto'}</label>
                            <div className="select-wrapper">
                                <select
                                    className="transact-pill-input"
                                    value={accountId}
                                    onChange={(e) => handleFromAccountChange(e.target.value)}
                                >
                                {accounts.map((acc) => (
                                    <option key={acc.id} value={acc.id}>
                                        {acc.name} — {formatKr(acc.balance)}
                                    </option>
                                ))}
                                </select>
                                <ChevronDown size={16} className="select-chevron" />
                            </div>
                        </div>

                        {mode === 'transfer' && (
                            <div className="transact-field">
                                <label className="transact-label">Till konto</label>
                                <div className="select-wrapper">
                                    <select
                                        className="transact-pill-input"
                                        value={selectedToAccount?.id ?? ''}
                                        onChange={(e) => setToAccountId(e.target.value)}
                                    >
                                    {transferToAccounts.map((acc) => (
                                        <option key={acc.id} value={acc.id}>
                                            {acc.name} — {formatKr(acc.balance)}
                                        </option>
                                    ))}
                                    </select>
                                    <ChevronDown size={16} className="select-chevron" />
                                </div>
                            </div>
                        )}

                        <div className="transact-field">
                            <label className="transact-label">Belopp (kr)</label>
                            <input
                                className="transact-pill-input"
                                type="text"
                                placeholder="0"
                                value={amount}
                                onChange={(e) => setAmount(e.target.value)}
                            />
                            <p
                                className={`transact-available-balance ${
                                    mode === 'deposit' ? 'transact-available-balance--empty' : ''
                                }`}
                            >
                                Tillgängligt: {selectedAccount ? formatKr(selectedAccount.balance) : '0 kr'}
                            </p>
                        </div>

                        <p
                            className={`transact-message ${
                                error
                                    ? 'transact-message--error'
                                    : receipt
                                      ? 'transact-message--success'
                                      : 'transact-message--empty'
                            }`}
                        >
                            {error || (receipt && <OrderReceipt receipt={receipt} />)}
                        </p>

                        <button type="submit" className="transact-submit-btn" disabled={isSubmitting}>
                            {isSubmitting
                                ? mode === 'deposit'
                                    ? 'Sätter in...'
                                    : mode === 'withdraw'
                                        ? 'Tar ut...'
                                        : 'Flyttar...'
                                : mode === 'deposit'
                                    ? 'Sätt in pengar'
                                    : mode === 'withdraw'
                                        ? 'Ta ut pengar'
                                        : 'Flytta pengar'}
                        </button>
                    </form>
                </div>

                <CustomerServiceFooter />
            </main>
        </div>
    )
}

export default TransactPage
