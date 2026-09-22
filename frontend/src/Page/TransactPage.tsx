import { type FormEvent, useState } from 'react'
import { UserProfile } from '../components/UserProfile'
import { useLogout } from '../hooks/useLogout'
import './TransactPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { mockTransactAccounts } from './mockTransactAccounts'
import { GraduationCap, ChevronDown } from 'lucide-react'
import { useTheme } from '../context/useTheme'

type Mode = 'deposit' | 'withdraw'

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE')} kr`
}

function TransactPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const [mode, setMode] = useState<Mode>('deposit')
    const [accountId, setAccountId] = useState(mockTransactAccounts[0].id)
    const [amount, setAmount] = useState('')
    const [error, setError] = useState('')
    const [success, setSuccess] = useState('')
    const [isSubmitting, setIsSubmitting] = useState(false)

    const selectedAccount = mockTransactAccounts.find((acc) => acc.id === accountId)!

    function handleModeChange(newMode: Mode) {
        setMode(newMode)
        setError('')
        setSuccess('')
    }

    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setSuccess('')

        const numericAmount = Number(amount.replace(',', '.'))

        if (!amount || Number.isNaN(numericAmount) || numericAmount <= 0) {
            setError('Ange ett giltigt belopp större än 0.')
            return
        }

        if (mode === 'withdraw' && numericAmount > selectedAccount.balance) {
            setError('Beloppet överstiger tillgängligt saldo.')
            return
        }

        setError('')
        setIsSubmitting(true)

        setTimeout(() => {
            setIsSubmitting(false)
            setSuccess(
                mode === 'deposit'
                    ? `${formatKr(numericAmount)} har satts in på ${selectedAccount.name}.`
                    : `${formatKr(numericAmount)} har tagits ut från ${selectedAccount.name}.`
            )
            setAmount('')
        }, 800)
    }

    return (
    <div className="dashboard-page">
        <DecorativeCircle color="orange" size={80} left={-20} top={170} />
        <DecorativeCircle color="blue" size={140} left={30} bottom={150} />
        <DecorativeCircle color="orange" size={170} left={180} bottom={40} />
        <DecorativeCircle color="green" size={120} right={90} top={110} opacity={0.9} />
        <DecorativeCircle color="blue" size={190} right={-40} top={40} />

      <div className="transact-brand-circle">
    <strong>nordiska<span className="brand-dot">.</span></strong>
        </div>      

    <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

    <main className="dashboard-main">
        <UserProfile />

        <div className="transact-content">
                <div className="transact-header">
                    <h1 tabIndex={0}>Flytta pengar</h1>
                    <p>Sätt in eller ta ut medel från dina sparkonton.</p>
                </div>

                    <div className="pill-toggle-row">
                        <button
                            type="button"
                            className={`pill-toggle ${mode === 'deposit' ? 'pill-toggle--active' : ''}`}
                            onClick={() => handleModeChange('deposit')}
                        >
                            Sätt in
                        </button>
                        <button
                            type="button"
                            className={`pill-toggle ${mode === 'withdraw' ? 'pill-toggle--active' : ''}`}
                            onClick={() => handleModeChange('withdraw')}
                        >
                            Ta ut
                        </button>
                    </div>

                    <div className="account-preview-card">
                        <div className="account-preview-header">
                            <GraduationCap size={16} />
                            <span>{selectedAccount.name.toUpperCase()}</span>
                        </div>
                        <p className="account-preview-value">{formatKr(selectedAccount.balance)}</p>
                    </div>

                    <form onSubmit={handleSubmit} className="transact-form-card">
                        <div className="transact-field">
                            <label className="transact-label">Konto</label>
                            <div className="select-wrapper">
                                <select
                                    className="transact-pill-input"
                                    value={accountId}
                                    onChange={(e) => setAccountId(e.target.value)}
                                >
                                    {mockTransactAccounts.map((acc) => (
                                        <option key={acc.id} value={acc.id}>
                                            {acc.name} — {formatKr(acc.balance)}
                                        </option>
                                    ))}
                                </select>
                                <ChevronDown size={16} className="select-chevron" />
                            </div>
                        </div>

                        <div className="transact-field">
                            <label className="transact-label">Belopp (kr)</label>
                            <input
                                className="transact-pill-input"
                                type="text"
                                placeholder="0"
                                value={amount}
                                onChange={(e) => setAmount(e.target.value)}
                            />
                        </div>

                        {error && <p className="transact-message transact-message--error">{error}</p>}
                        {success && <p className="transact-message transact-message--success">{success}</p>}

                        <button type="submit" className="transact-submit-btn" disabled={isSubmitting}>
                            {isSubmitting
                                ? mode === 'deposit' ? 'Sätter in...' : 'Tar ut...'
                                : mode === 'deposit' ? 'Sätt in pengar' : 'Ta ut pengar'}
                        </button>
                    </form>
                </div>
            </main>
        </div>
    )
}

export default TransactPage
