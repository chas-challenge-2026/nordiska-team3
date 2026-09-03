import { type FormEvent, useState } from 'react'
import './LoginPage.css'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'
import { DecorativeCircle } from '../../components/DecorativeCircle'

function LoginPage() {
    const [isCheckingPin, setIsCheckingPin] = useState(false)
    const [isStartingBankId, setIsStartingBankId] = useState(false)
    const [showBankIdOptions, setShowBankIdOptions] = useState(false)
    const [bankIdMode, setBankIdMode] = useState('')
    const [personalNumber, setPersonalNumber] = useState('')
    const [pin, setPin] = useState('')
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
    }

    return (
        <main className="login-page">
            <section className="login-shell">
                <DecorativeCircle color="orange" size={190} left={-30} top={220} />
                <DecorativeCircle color="green" size={115} left={80} top={340} opacity={0.9} />
                <DecorativeCircle color="orange" size={75} left={150} bottom={80} />
                <DecorativeCircle color="green" size={150} right={-20} bottom={180} />
                <DecorativeCircle color="blue" size={110} right={60} bottom={100} opacity={0.9} />

                <div className="theme-toggle-placeholder" aria-label="Byt tema">
                </div>

                <div className="brand-card">
                    <span>Sparportal</span>
                    <strong>nordiska</strong>
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

                    <Input
                        label="PIN-kod"
                        type="password"
                        placeholder="••••••"
                        value={pin}
                        onChange={(event) => setPin(event.target.value)}
                    />

                    <p className="login-error">
                        {errorMessage}
                    </p>

                    <Button type="submit" variant="primary">
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
                        <Button type="button" variant="secondary" onClick={handleBankIdLogin}>
                            {isStartingBankId
                                ? bankIdMode === 'same-device'
                                    ? 'Öppnar BankID...'
                                    : 'Väntar på BankID...'
                                : 'Logga in med BankID'}
                        </Button>
                    )}

                    <p className="secure-text">
                        Inloggning skyddas av BankID och 256-bitars kryptering.
                    </p>
                </form>
            </section>
        </main>
    )
}

export default LoginPage
