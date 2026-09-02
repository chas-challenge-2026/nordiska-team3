import './LoginPage.css'

function LoginPage() {
    return (
        <main className="login-page">
            <section className="login-shell">
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

                    <label>
                        Personnummer
                        <input type="text" placeholder="ÅÅMMDD-XXXX" />
                    </label>

                    <label>
                        PIN-kod
                        <input type="password" placeholder="••••••" />
                    </label>

                    <div className="button-placeholder primary">Logga in</div>
                    <div className="button-placeholder secondary">Logga in med BankID</div>

                    <p className="secure-text">
                        Inloggning skyddas av BankID och 256-bitars kryptering.
                    </p>
                </form>
            </section>
        </main>
    )
}

export default LoginPage