import './LoginPage.css'
import { Button } from '../../components/Button'
import { Input } from '../../components/Input'

function LoginPage() {
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

                <form className="login-card">
                    <div>
                        <h1>Välkommen tillbaka</h1>
                        <p>Logga in med BankID eller PIN.</p>
                    </div>

                    <Input label="Personnummer" type="text" placeholder="ÅÅMMDD-XXXX" />

                    <Input label="PIN-kod" type="password" placeholder="••••••" />

                    <Button type="submit" variant="primary">
                        Logga in
                    </Button>

                    <Button type="button" variant="secondary">
                        Logga in med BankID
                    </Button>

                    <p className="secure-text">
                        Inloggning skyddas av BankID och 256-bitars kryptering.
                    </p>
                </form>
            </section>
        </main>
    )
}

export default LoginPage
