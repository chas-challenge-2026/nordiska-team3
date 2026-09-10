import { useNavigate } from 'react-router-dom'
import './DashboardPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { BalanceOverview } from '../components/DashboardActions/BalanceOverview'
import { DashboardActions } from '../components/DashboardActions/DashboardActions'
import { RecentEvents } from '../components/RecentEvents/RecentEvents'
import { mockAccounts } from '../components/DashboardActions/mockAccounts'
import type { DashboardAction } from '../components/DashboardActions/mockDashboardActions'
import { useTheme } from '../context/useTheme'

function DashboardPage() {
    const navigate = useNavigate()
    const { toggleTheme } = useTheme()

    const currentUser = {
        name: 'Emma Lindström',
        email: 'emma@exempel.se',
    }

    function handleLogout() {
        navigate('/login')
    }

    function handleAccountClick(accountId: string) {
        console.log('Öppna konto:', accountId)
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

    return (
        <div className="dashboard-page">
            <DecorativeCircle color="orange" size={170} left={-40} top={200} />
            <DecorativeCircle color="blue" size={100} left={20} top={310} opacity={0.9} />
            <DecorativeCircle color="orange" size={60} left={280} bottom={60} />
            <DecorativeCircle color="green" size={140} right={-30} bottom={140} />
            <DecorativeCircle color="green" size={90} right={40} bottom={30} opacity={0.9} />

            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <main className="dashboard-main">
                <div className="user-profile">
                    <div className="user-profile-avatar">{currentUser.name.charAt(0)}</div>
                    <div className="user-profile-text">
                        <p className="user-profile-name">{currentUser.name}</p>
                        <p className="user-profile-email">{currentUser.email}</p>
                    </div>
                </div>

                <div className="dashboard-content">
                    <div className="dashboard-brand-card">
                        <span>Sparportal</span>
                        <strong>nordiska<span className="dashboard-brand-dot">.</span></strong>
                    </div>

                    <BalanceOverview
                        totalLabel="TOTALT SPARAT"
                        totalValue="136 571 kr"
                        subLabel="3 konton · snitt 3,2 % ränta"
                        accounts={mockAccounts}
                        onAccountClick={handleAccountClick}
                    />

                    <RecentEvents />

                    <DashboardActions onActionClick={handleActionClick} />
                </div>
            </main>
        </div>
    )
}

export default DashboardPage