import './DashboardPage.css'
import { NavIcon } from '../components/NavIcon'
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
    return (
        <div className="dashboard-page">
            {/* Sidomeny - desktop */}
            <aside className="dashboard-sidebar">
                <NavIcon icon={<LayoutGrid size={18} />} label="Dashboard" active />
                <NavIcon icon={<ArrowLeftRight size={18} />} label="Insättning/Uttag" />
                <NavIcon icon={<History size={18} />} label="Historik" />
                <NavIcon icon={<FileText size={18} />} label="Skatterapport" />
                <NavIcon icon={<HelpCircle size={18} />} label="FAQ" />
                <div className="sidebar-divider" />
                <NavIcon icon={<Moon size={18} />} label="Byt tema" />
                <NavIcon icon={<LogOut size={18} />} label="Logga ut" />
            </aside>

            <main className="dashboard-main">
                {/* Saldo-kort och kontokort byggs i #13b */}
            </main>

            {/* Bottennav - mobil */}
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