import { useEffect, useState, type ReactNode } from 'react'
import { ChevronRight } from 'lucide-react'
import './BalanceOverview.css'

export interface Account {
    id: string
    icon: ReactNode
    label: string
    value: string
    variant?: 'default' | 'accent' | 'success' | 'danger' | 'purple' | 'pink'
}

interface BalanceOverviewProps {
    totalLabel: string
    totalValue: string
    subLabel: string
    accounts: Account[]
    onAccountClick?: (accountId: string) => void
}

function parseKrValue(value: string) {
    return Number(value.replace(/\s/g, '').replace('kr', '').replace(',', '.'))
}

function formatKrValue(value: number) {
    return `${Math.round(value).toLocaleString('sv-SE')} kr`
}

export function BalanceOverview({
    totalLabel,
    totalValue,
    subLabel,
    accounts,
    onAccountClick,
}: BalanceOverviewProps) {
    const totalAmount = parseKrValue(totalValue)
    const [animatedTotal, setAnimatedTotal] = useState(Number.isFinite(totalAmount) ? 0 : totalValue)

    useEffect(() => {
        if (!Number.isFinite(totalAmount)) {
            setAnimatedTotal(totalValue)
            return
        }

        let animationFrameId = 0
        const duration = 450
        const startTime = performance.now()

        function animate(currentTime: number) {
            const elapsedTime = currentTime - startTime
            const progress = Math.min(elapsedTime / duration, 1)
            const easedProgress = 1 - Math.pow(1 - progress, 3)

            setAnimatedTotal(formatKrValue(totalAmount * easedProgress))

            if (progress < 1) {
                animationFrameId = requestAnimationFrame(animate)
            }
        }

        animationFrameId = requestAnimationFrame(animate)

        return () => cancelAnimationFrame(animationFrameId)
    }, [totalAmount, totalValue])

    return (
        <div className="balance-overview">
            <div className="balance-card">
                <p className="balance-label">{totalLabel}</p>
                <p className="balance-value">{animatedTotal}</p>
                <p className="balance-sub">{subLabel}</p>
            </div>

            <div className="accounts-row">
                {accounts.map((account) => (
                    <button
                        key={account.id}
                        className="account-card"
                        onClick={() => onAccountClick?.(account.id)}
                    >
                        <div className={`account-card-header account-card-header--${account.variant ?? 'default'}`}>
                            {account.icon}
                            <span>{account.label}</span>
                        </div>
                        <div className="account-card-footer">
                            <p className={`account-card-value account-card-value--${account.variant ?? 'default'}`}>
                                {account.value}
                            </p>
                            <ChevronRight
                                size={16}
                                className={`account-card-chevron account-card-chevron--${account.variant ?? 'default'}`}
                            />
                        </div>
                    </button>
                ))}
            </div>
        </div>
    )
}
