import './PlaceholderPage.css'
import { AppNav } from '../components/AppNav'
import { UserProfile } from '../components/UserProfile'
import { useLogout } from '../hooks/useLogout'

function FaqPage() {
    const handleLogout = useLogout()

    return (
        <div className="placeholder-page">
            <AppNav onLogout={handleLogout} />
            <main className="placeholder-main">
                <UserProfile />

                <h1 tabIndex={0}>FAQ</h1>
                <p>Kommer snart.</p>
            </main>
        </div>
    )
}

export default FaqPage
