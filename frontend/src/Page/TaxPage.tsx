import './PlaceholderPage.css'
import { AppNav } from '../components/AppNav'
import { useNavigate } from 'react-router-dom'

function TaxPage() {
    const navigate = useNavigate()
    return (
        <div className="placeholder-page">
            <AppNav onLogout={() => navigate('/login')} />
            <main className="placeholder-main">
                <h1>Skatterapport</h1>
                <p>Kommer snart.</p>
            </main>
        </div>
    )
}

export default TaxPage