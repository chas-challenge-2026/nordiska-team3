import type { ReactNode } from 'react'
import { Clock3, FileText, Minus, Plus } from 'lucide-react'
import './DashboardActions.css'

export type DashboardAction = {
  id: string
  label: string
  variant: 'primary' | 'secondary'
  action: 'deposit' | 'withdraw' | 'history' | 'tax'
}

const defaultDashboardActions: DashboardAction[] = [
  {
    id: 'deposit',
    label: 'Sätt in',
    variant: 'secondary',
    action: 'deposit',
  },
  {
    id: 'withdraw',
    label: 'Ta ut',
    variant: 'primary',
    action: 'withdraw',
  },
  {
    id: 'history',
    label: 'Historik',
    variant: 'secondary',
    action: 'history',
  },
  {
    id: 'tax',
    label: 'Skatt',
    variant: 'secondary',
    action: 'tax',
  },
]

type DashboardActionsProps = {
  actions?: DashboardAction[]
  onActionClick?: (action: DashboardAction['action']) => void
}

const actionIcons: Record<DashboardAction['action'], ReactNode> = {
  deposit: <Plus size={16} strokeWidth={3} />,
  withdraw: <Minus size={16} strokeWidth={3} />,
  history: <Clock3 size={16} strokeWidth={2.5} />,
  tax: <FileText size={16} strokeWidth={2.5} />,
}

export function DashboardActions({
  actions = defaultDashboardActions,
  onActionClick,
}: DashboardActionsProps) {
  return (
    <nav className="dashboard-actions" aria-label="Dashboard actions">
      {actions.map((action) => (
        <button
          className={`dashboard-actions__button dashboard-actions__button--${action.variant} dashboard-actions__button--${action.action}`}
          type="button"
          key={action.id}
          onClick={() => onActionClick?.(action.action)}
        >
          <span className="dashboard-actions__icon" aria-hidden="true">
            {actionIcons[action.action]}
          </span>
          <span>{action.label}</span>
        </button>
      ))}
    </nav>
  )
}
