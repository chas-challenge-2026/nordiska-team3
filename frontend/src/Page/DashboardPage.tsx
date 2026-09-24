import { useState } from 'react'
import { UserProfile } from '../components/UserProfile'
import './DashboardPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { BalanceOverview } from '../components/DashboardActions/BalanceOverview'
import { DashboardActions } from '../components/DashboardActions/DashboardActions'
import { RecentEvents } from '../components/RecentEvents/RecentEvents'
import { mockAccounts } from '../components/DashboardActions/mockAccounts'
import {
    mockDashboardAccountTransactions,
    type DashboardAccountTransaction,
} from '../components/DashboardActions/mockDashboardAccountTransactions'
import type { Account } from '../components/DashboardActions/BalanceOverview'
import type { DashboardAction } from '../components/DashboardActions/mockDashboardActions'
import { useLogout } from '../hooks/useLogout'
import { useTheme } from '../context/useTheme'
import { Modal } from '../components/Modal'
import { Input } from '../components/Input'
import { Button } from '../components/Button'
import {
    Banknote,
    Briefcase,
    Car,
    Gem,
    Gift,
    GraduationCap,
    Heart,
    Home,
    Landmark,
    Laptop,
    PiggyBank,
    Plane,
    Plus,
    Shield,
    Target,
    Umbrella,
    Wallet,
} from 'lucide-react'

type CreateAccountIconId =
    | 'piggyBank'
    | 'graduationCap'
    | 'plane'
    | 'shield'
    | 'wallet'
    | 'home'
    | 'car'
    | 'gift'
    | 'heart'
    | 'briefcase'
    | 'laptop'
    | 'target'
    | 'umbrella'
    | 'landmark'
    | 'banknote'
    | 'gem'
type CreateAccountVariant = NonNullable<Account['variant']>

const createAccountIconOptions: { id: CreateAccountIconId; label: string; icon: JSX.Element }[] = [
    { id: 'piggyBank', label: 'Sparande', icon: <PiggyBank size={16} /> },
    { id: 'graduationCap', label: 'Studier', icon: <GraduationCap size={16} /> },
    { id: 'plane', label: 'Resa', icon: <Plane size={16} /> },
    { id: 'shield', label: 'Buffert', icon: <Shield size={16} /> },
    { id: 'wallet', label: 'Plånbok', icon: <Wallet size={16} /> },
    { id: 'home', label: 'Bostad', icon: <Home size={16} /> },
    { id: 'car', label: 'Bil', icon: <Car size={16} /> },
    { id: 'gift', label: 'Presenter', icon: <Gift size={16} /> },
    { id: 'heart', label: 'Familj', icon: <Heart size={16} /> },
    { id: 'briefcase', label: 'Jobb', icon: <Briefcase size={16} /> },
    { id: 'laptop', label: 'Teknik', icon: <Laptop size={16} /> },
    { id: 'target', label: 'Mål', icon: <Target size={16} /> },
    { id: 'umbrella', label: 'Trygghet', icon: <Umbrella size={16} /> },
    { id: 'landmark', label: 'Bank', icon: <Landmark size={16} /> },
    { id: 'banknote', label: 'Pengar', icon: <Banknote size={16} /> },
    { id: 'gem', label: 'Lyx', icon: <Gem size={16} /> },
]

const createAccountVariantOptions: { value: CreateAccountVariant; label: string }[] = [
    { value: 'default', label: 'Blå' },
    { value: 'accent', label: 'Orange' },
    { value: 'success', label: 'Grön' },
    { value: 'danger', label: 'Röd' },
    { value: 'purple', label: 'Lila' },
    { value: 'pink', label: 'Rosa' },
]

function parseKr(value: string) {
    return Number(value.replace(/\s/g, '').replace('kr', '').replace(',', '.'))
}

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE')} kr`
}

function formatAccountName(value: string) {
    return value.toLocaleLowerCase('sv-SE').replace(/(^|\s)\S/g, (letter) => letter.toLocaleUpperCase('sv-SE'))
}

function DashboardPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const [accounts, setAccounts] = useState<Account[]>(mockAccounts)
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
    const [newAccountName, setNewAccountName] = useState('')
    const [newAccountIconId, setNewAccountIconId] = useState<CreateAccountIconId>('piggyBank')
    const [newAccountVariant, setNewAccountVariant] = useState<CreateAccountVariant>('default')
    const [selectedAccount, setSelectedAccount] = useState<Account | null>(null)
    const [modalTransactionType, setModalTransactionType] = useState<'deposit' | 'withdrawal'>('deposit')
    const [modalAmount, setModalAmount] = useState('')
    const [modalTransactionMessage, setModalTransactionMessage] = useState('')
    const [accountTransactions, setAccountTransactions] = useState<DashboardAccountTransaction[]>(
        mockDashboardAccountTransactions
    )

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
    }))

    function handleAccountClick(accountId: string) {
        const account = accounts.find((account) => account.id === accountId)

        if (!account) return

        setSelectedAccount(account)
        setModalTransactionMessage('')
    }

    function handleModalTransactionTypeChange(type: 'deposit' | 'withdrawal') {
        setModalTransactionType(type)
        setModalTransactionMessage('')
    }

    function handleActionClick(action: DashboardAction['action']) {
        switch (action) {
            case 'deposit':
            case 'withdraw':
                console.log('Navigera till insättning/uttag:', action)
                break
            case 'history':
                console.log('Navigera till historik')
                break
            case 'tax':
                console.log('Navigera till skatterapport')
                break
        }
    }

    function handleCreateAccount() {
        if (!newAccountName.trim()) return

        const selectedIcon = createAccountIconOptions.find((option) => option.id === newAccountIconId)

        const newAccount: Account = {
            id: crypto.randomUUID(),
            icon: selectedIcon?.icon ?? <PiggyBank size={16} />,
            label: newAccountName.toUpperCase(),
            value: '0 kr',
            variant: newAccountVariant,
        }

        setAccounts((prev) => [...prev, newAccount])
        setNewAccountName('')
        setNewAccountIconId('piggyBank')
        setNewAccountVariant('default')
        setIsCreateModalOpen(false)
    }

    function handleModalTransaction() {
        if (!selectedAccount) return

        const amount = Number(modalAmount.replace(',', '.'))

        if (!modalAmount || Number.isNaN(amount) || amount <= 0) {
            setModalTransactionMessage('Ange ett giltigt belopp större än 0 kr.')
            return
        }

        const currentBalance = parseKr(selectedAccount.value)

        if (modalTransactionType === 'withdrawal' && amount > currentBalance) {
            setModalTransactionMessage('Beloppet överstiger tillgängligt saldo.')
            return
        }

        const balanceChange = modalTransactionType === 'deposit' ? amount : -amount
        const updatedAccount: Account = {
            ...selectedAccount,
            value: formatKr(currentBalance + balanceChange),
        }

        const newTransaction: DashboardAccountTransaction = {
            id: crypto.randomUUID(),
            accountId: selectedAccount.id,
            title: modalTransactionType === 'deposit' ? 'Insättning' : 'Uttag',
            date: new Intl.DateTimeFormat('sv-SE', {
                day: 'numeric',
                month: 'short',
                year: 'numeric',
            }).format(new Date()),
            amount: balanceChange,
            type: modalTransactionType,
        }

        setAccounts((prevAccounts) =>
            prevAccounts.map((account) => (account.id === selectedAccount.id ? updatedAccount : account))
        )
        setSelectedAccount(updatedAccount)
        setAccountTransactions((prevTransactions) => [newTransaction, ...prevTransactions])
        setModalTransactionMessage(
            modalTransactionType === 'deposit'
                ? `${formatKr(amount)} har satts in på kontot.`
                : `${formatKr(amount)} har tagits ut från kontot.`
        )

        setModalAmount('')
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
                    <div className="dashboard-brand-card">
                        <span>Sparportal</span>
                        <strong>nordiska<span className="dashboard-brand-dot">.</span></strong>
                    </div>

                    <BalanceOverview
                        totalLabel="TOTALT SPARAT"
                        totalValue="136 571 kr"
                        subLabel="3 konton · snitt 3,2 % ränta"
                        accounts={accounts}
                        onAccountClick={handleAccountClick}
                    />

                    <button
                        type="button"
                        className="create-account-btn"
                        onClick={() => setIsCreateModalOpen(true)}
                    >
                        <Plus size={16} />
                        Skapa nytt sparkonto
                    </button>

                    <Modal
                        isOpen={isCreateModalOpen}
                        onClose={() => setIsCreateModalOpen(false)}
                        title="Skapa nytt sparkonto"
                    >
                        <div className="create-account-modal">
                            <Input
                                label="Kontonamn"
                                type="text"
                                placeholder="T.ex. Resekassa"
                                value={newAccountName}
                                onChange={(e) => setNewAccountName(e.target.value)}
                            />

                            <div className="create-account-modal__field">
                                <p>Ikon</p>
                                <div className="create-account-modal__option-grid">
                                    {createAccountIconOptions.map((option) => (
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
                                    {createAccountVariantOptions.map((option) => (
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
                        onClose={() => setSelectedAccount(null)}
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
                                </div>

                                {selectedAccountEvents.length > 0 ? (
                                    <RecentEvents events={selectedAccountEvents} />
                                ) : (
                                    <p className="account-modal__empty">Inga händelser hittades för kontot.</p>
                                )}

                                <div className="account-modal__section account-modal__transfer">
                                    <h3>Flytta pengar</h3>

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
                                                onChange={(e) => setModalAmount(e.target.value)}
                                            />
                                        </div>

                                        {modalTransactionMessage && (
                                            <p className="transact-message account-modal__message">
                                                {modalTransactionMessage}
                                            </p>
                                        )}

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
                    <RecentEvents />

                    <DashboardActions onActionClick={handleActionClick} />
                </div>
            </main>
        </div>
    )
}

export default DashboardPage
