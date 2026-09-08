import { useLocation, useNavigate } from 'react-router-dom'
import { NavIcon } from './NavIcon'
import {
    LayoutGrid,
    ArrowLeftRight,
    History,
    FileText,
    HelpCircle,
    Moon,
    Sun,
    LogOut,
} from 'lucide-react'
import './AppNav.css'
import { useTheme } from '../context/useTheme'

interface AppNavProps {
    onLogout: () => void
    onThemeToggle?: () => void
}

const navItems = [
    { to: '/dashboard', icon: LayoutGrid, label: 'Dashboard' },
    { to: '/transaktioner', icon: ArrowLeftRight, label: 'Insättning/Uttag' },
    { to: '/historik', icon: History, label: 'Historik' },
    { to: '/skatt', icon: FileText, label: 'Skatterapport' },
    { to: '/faq', icon: HelpCircle, label: 'FAQ' },
]

export function AppNav({ onLogout, onThemeToggle }: AppNavProps) {
    const location = useLocation()
    const navigate = useNavigate()
    const { theme } = useTheme()
    const ThemeIcon = theme === 'dark' ? Sun : Moon
    const themeLabel = theme === 'dark' ? 'Byt till ljust tema' : 'Byt till mörkt tema'

    function renderItems() {
        return navItems.map(({ to, icon: Icon, label }) => (
            <NavIcon
                key={to}
                icon={<Icon size={18} />}
                label={label}
                active={location.pathname.startsWith(to)}
                onClick={() => navigate(to)}
            />
        ))
    }

    return (
        <>
            <aside className="app-nav app-nav--sidebar">
                {renderItems()}
                <div className="sidebar-divider" />
                <NavIcon icon={<ThemeIcon size={18} />} label={themeLabel} onClick={onThemeToggle} />
                <NavIcon icon={<LogOut size={18} />} label="Logga ut" onClick={onLogout} />
            </aside>

            <nav className="app-nav app-nav--bottom">
                {renderItems()}
                <div className="bottom-nav-divider" />
                <NavIcon icon={<ThemeIcon size={18} />} label={themeLabel} onClick={onThemeToggle} />
                <NavIcon icon={<LogOut size={18} />} label="Logga ut" onClick={onLogout} />
            </nav>
        </>
    )
}
