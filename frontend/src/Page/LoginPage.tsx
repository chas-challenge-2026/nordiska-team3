import { type FormEvent, useState } from 'react'
import { Eye, EyeOff, Moon, PlusSquare, ShieldCheck, Sun } from 'lucide-react'
import { useNavigate } from 'react-router-dom'
import './LoginPage.css'
import { Button } from '../components/Button'
import { Input } from '../components/Input'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { useTheme } from '../context/useTheme'

function LoginPage() {
    const navigate = useNavigate()
    const { theme, toggleTheme } = useTheme()

    const [isCheckingPin, setIsCheckingPin] = useState(false)
    const [isStartingBankId, setIsStartingBankId] = useState(false)
    const [showBankIdOptions, setShowBankIdOptions] = useState(false)
    const [bankIdMode, setBankIdMode] = useState('')
    const [personalNumber, setPersonalNumber] = useState('')
    const [pin, setPin] = useState('')
    const [showPin, setShowPin] = useState(false)
    const [errorMessage, setErrorMessage] = useState('')

    function handlePinLogin(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()
        setIsStartingBankId(false)

        if (!personalNumber || !pin) {
            setErrorMessage('Fyll i personnummer och PIN-kod.')
            return
        }

        setErrorMessage('')
        setIsCheckingPin(true)

        // Mock: simulerar en API-fördröjning innan vi navigerar vidare
        setTimeout(() => {
            setIsCheckingPin(false)
            navigate('/dashboard')
        }, 900)
    }

    function handleBankIdLogin() {
        setIsCheckingPin(false)
        setIsStartingBankId(false)
        setErrorMessage('')
        setShowBankIdOptions(true)
    }

    function startBankIdLogin(mode: 'same-device' | 'other-device') {
        setBankIdMode(mode)
        setShowBankIdOptions(false)
        setIsStartingBankId(true)

        // Mock: simulerar att BankID-appen bekräftats efter en kort väntan
        setTimeout(() => {
            setIsStartingBankId(false)
            navigate('/dashboard')
        }, 1400)
    }

    return (
        <main className="login-page">
            <section className="login-shell">
                <DecorativeCircle color="orange" size={190} left={-30} top={220} />
                <DecorativeCircle color="green" size={115} left={80} top={340} opacity={0.9} />
                <DecorativeCircle color="orange" size={75} left={150} bottom={80} />
                <DecorativeCircle color="green" size={150} right={-20} bottom={180} />
                <DecorativeCircle color="blue" size={110} right={60} bottom={100} opacity={0.9} />

                <button
                    className="theme-toggle"
                    type="button"
                    aria-label={theme === 'dark' ? 'Byt till ljust tema' : 'Byt till mörkt tema'}
                    onClick={toggleTheme}
                >
                    {theme === 'dark' ? <Sun size={16} /> : <Moon size={16} />}
                </button>

                <div className="login-stack">
                    <div className="login-brand-card">
                        <span>Sparportal</span>
                        <strong>nordiska<span className="login-brand-dot">.</span></strong>
                    </div>

                    <form className="login-card" onSubmit={handlePinLogin}>
                    <div>
                        <h1>Välkommen tillbaka</h1>
                        <p>Logga in med BankID eller PIN.</p>
                    </div>

                    <Input
                        label="Personnummer"
                        type="text"
                        placeholder="ÅÅMMDD-XXXX"
                        value={personalNumber}
                        onChange={(event) => setPersonalNumber(event.target.value)}
                    />

                    <div className="input-group">
                        <label className="input-label">PIN-kod</label>
                        <div className="password-input-wrapper">
                            <input
                                className="input"
                                type={showPin ? 'text' : 'password'}
                                placeholder="••••••"
                                value={pin}
                                onChange={(event) => setPin(event.target.value)}
                            />
                            <button
                                className="password-toggle"
                                type="button"
                                aria-label={showPin ? 'Dölj PIN-kod' : 'Visa PIN-kod'}
                                onClick={() => setShowPin(!showPin)}
                            >
                                {showPin ? <EyeOff size={16} /> : <Eye size={16} />}
                            </button>
                        </div>
                    </div>

                    <p className="login-error">
                        {errorMessage}
                    </p>

                    <Button type="submit" variant="primary" disabled={isCheckingPin}>
                        {isCheckingPin ? 'Kontrollerar PIN...' : 'Logga in'}
                    </Button>

                    {showBankIdOptions ? (
                        <div className="bankid-options">
                            <button type="button" onClick={() => startBankIdLogin('same-device')}>
                                Samma enhet
                            </button>
                            <button type="button" onClick={() => startBankIdLogin('other-device')}>
                                Annan enhet
                            </button>
                        </div>
                    ) : (
                        <Button type="button" variant="secondary" onClick={handleBankIdLogin} disabled={isStartingBankId}>
                            {!isStartingBankId && <PlusSquare size={16} />}
                            {isStartingBankId
                                ? bankIdMode === 'same-device'
                                    ? 'Öppnar BankID...'
                                    : 'Väntar på BankID...'
                                : 'Logga in med BankID'}
                        </Button>
                    )}

                    <p className="secure-text">
                        <ShieldCheck size={16} />
                        <span>Inloggning skyddas av BankID och 256-bitars kryptering.</span>
                    </p>
                    </form>
                </div>
            </section>
        </main>
    )
}

export default LoginPage
