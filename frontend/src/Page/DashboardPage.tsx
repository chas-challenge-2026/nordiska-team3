import { useState } from 'react'
import { UserProfile } from '../components/UserProfile'
import './DashboardPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { BalanceOverview } from '../components/DashboardActions/BalanceOverview'
import { DashboardActions } from '../components/DashboardActions/DashboardActions'
import { RecentEvents } from '../components/RecentEvents/RecentEvents'
import { mockAccounts } from '../components/DashboardActions/mockAccounts'
import type { Account } from '../components/DashboardActions/BalanceOverview'
import type { DashboardAction } from '../components/DashboardActions/mockDashboardActions'
import { useLogout } from '../hooks/useLogout'
import { useTheme } from '../context/useTheme'
import { Modal } from '../components/Modal'
import { Input } from '../components/Input'
import { Button } from '../components/Button'
import { PiggyBank, Plus } from 'lucide-react'

function DashboardPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const [accounts, setAccounts] = useState<Account[]>(mockAccounts)
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false)
    const [newAccountName, setNewAccountName] = useState('')

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

    function handleCreateAccount() {
        if (!newAccountName.trim()) return

        const newAccount: Account = {
            id: crypto.randomUUID(),
            icon: <PiggyBank size={16} />,
            label: newAccountName.toUpperCase(),
            value: '0 kr',
            variant: 'default',
        }

        setAccounts((prev) => [...prev, newAccount])
        setNewAccountName('')
        setIsCreateModalOpen(false)
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
                        <Input
                            label="Kontonamn"
                            type="text"
                            placeholder="T.ex. Resekassa"
                            value={newAccountName}
                            onChange={(e) => setNewAccountName(e.target.value)}
                        />
                        <div style={{ marginTop: 16 }}>
                            <Button type="button" variant="primary" onClick={handleCreateAccount}>
                                Skapa konto
                            </Button>
                        </div>
                    </Modal>

                    <RecentEvents />

                    <DashboardActions onActionClick={handleActionClick} />
                </div>
            </main>
        </div>
    )
}

export default DashboardPage