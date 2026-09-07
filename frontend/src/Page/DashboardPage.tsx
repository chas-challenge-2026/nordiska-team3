import { useNavigate } from 'react-router-dom'
import './DashboardPage.css'
import { NavIcon } from '../components/NavIcon'
import { DecorativeCircle } from '../components/DecorativeCircle'
import {
    LayoutGrid,
    ArrowLeftRight,
    History,
    FileText,
    HelpCircle,
    Moon,
    LogOut,
} from 'lucide-react'

function DashboardPage() {
    const navigate = useNavigate()

    function handleLogout() {
        navigate('/login')
    }

    const currentUser = {
        name: 'Emma Lindström',
        email: 'emma@exempel.se',
    }

    return (
        <div className="dashboard-page">
            <aside className="dashboard-sidebar">
                <NavIcon icon={<LayoutGrid size={18} />} label="Dashboard" active />
                <NavIcon icon={<ArrowLeftRight size={18} />} label="Insättning/Uttag" />
                <NavIcon icon={<History size={18} />} label="Historik" />
                <NavIcon icon={<FileText size={18} />} label="Skatterapport" />
                <NavIcon icon={<HelpCircle size={18} />} label="FAQ" />
                <div className="sidebar-divider" />
                <NavIcon icon={<Moon size={18} />} label="Byt tema" />
                <NavIcon icon={<LogOut size={18} />} label="Logga ut" onClick={handleLogout} />
            </aside>

            <main className="dashboard-main">
                <DecorativeCircle color="orange" size={170} left={-40} top={200} />
                <DecorativeCircle color="blue" size={100} left={20} top={310} opacity={0.9} />
                <DecorativeCircle color="orange" size={60} left={280} bottom={60} />
                <DecorativeCircle color="green" size={140} right={-30} bottom={140} />
                <DecorativeCircle color="green" size={90} right={40} bottom={30} opacity={0.9} />

                <div className="user-profile">
                    <div className="user-profile-avatar">{currentUser.name.charAt(0)}</div>
                    <div className="user-profile-text">
                        <p className="user-profile-name">{currentUser.name}</p>
                        <p className="user-profile-email">{currentUser.email}</p>
                    </div>
                </div>

                {/* Saldo-kort och kontokort byggs snart */}
            </main>

            <nav className="dashboard-bottom-nav">
                <NavIcon icon={<LayoutGrid size={18} />} label="Dashboard" active />
                <NavIcon icon={<ArrowLeftRight size={18} />} label="Insättning/Uttag" />
                <NavIcon icon={<History size={18} />} label="Historik" />
                <NavIcon icon={<FileText size={18} />} label="Skatterapport" />
                <NavIcon icon={<HelpCircle size={18} />} label="FAQ" />
                <div className="bottom-nav-divider" />
                <NavIcon icon={<Moon size={18} />} label="Byt tema" />
            </nav>
        </div>
    )
}

export default DashboardPage