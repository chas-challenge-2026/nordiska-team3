import './PlaceholderPage.css'
import { AppNav } from '../components/AppNav'
import { useLogout } from '../hooks/useLogout'

function TaxPage() {
    const handleLogout = useLogout()

    return (
        <div className="placeholder-page">
            <AppNav onLogout={handleLogout} />
            <main className="placeholder-main">
                <h1>Skatterapport</h1>
                <p>Kommer snart.</p>
            </main>
        </div>
    )
}

export default TaxPage