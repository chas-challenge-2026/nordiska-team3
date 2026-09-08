import type { ReactNode } from 'react'
import { ChevronRight } from 'lucide-react'
import './BalanceOverview.css'

export interface Account {
    id: string
    icon: ReactNode
    label: string
    value: string
    variant?: 'default' | 'accent' | 'success'
}

interface BalanceOverviewProps {
    totalLabel: string
    totalValue: string
    subLabel: string
    accounts: Account[]
    onAccountClick?: (accountId: string) => void
}

export function BalanceOverview({
    totalLabel,
    totalValue,
    subLabel,
    accounts,
    onAccountClick,
}: BalanceOverviewProps) {
    return (
        <div className="balance-overview">
            <div className="balance-card">
                <p className="balance-label">{totalLabel}</p>
                <p className="balance-value">{totalValue}</p>
                <p className="balance-sub">{subLabel}</p>
            </div>

            <div className="accounts-row">
                {accounts.map((account) => (
                    <button
                        key={account.id}
                        className="account-card"
                        onClick={() => onAccountClick?.(account.id)}
                    >
                        <div className="account-card-header">
                            {account.icon}
                            <span>{account.label}</span>
                        </div>
                        <div className="account-card-footer">
                            <p className={`account-card-value account-card-value--${account.variant ?? 'default'}`}>
                                {account.value}
                            </p>
                            <ChevronRight size={16} />
                        </div>
                    </button>
                ))}
            </div>
        </div>
    )
}