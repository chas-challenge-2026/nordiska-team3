export type DashboardAction = {
  id: string
  label: string
  variant: 'primary' | 'secondary'
  action: 'deposit' | 'withdraw' | 'history' | 'tax'
}

export const mockDashboardActions: DashboardAction[] = [
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
