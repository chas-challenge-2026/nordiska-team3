import { useEffect, useState, type ReactNode } from 'react'
import { ChevronRight, Info } from 'lucide-react'
import { interestTypeExplanations, type InterestType } from '../../utils/accountInterest'
import './BalanceOverview.css'

export interface Account {
    id: string
    icon: ReactNode
    label: string
    value: string
    interest?: string
    interestRate?: string
    interestType?: InterestType
    // Ex: "123,45 kr"
    accruedInterest?: string
    accountNumber?: string
    variant?: 'default' | 'accent' | 'success' | 'danger' | 'purple' | 'pink'
}

interface BalanceOverviewProps {
    totalLabel: string
    totalValue: string
    subLabel: string
    accounts: Account[]
    interestSummary?: (animationProgress: number) => ReactNode
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
    interestSummary,
    onAccountClick,
}: BalanceOverviewProps) {
    const totalAmount = parseKrValue(totalValue)
    const [animation, setAnimation] = useState({ target: totalAmount, balance: 0, interest: 0 })
    const balanceProgress = animation.target === totalAmount ? animation.balance : 0
    const interestProgress = Number.isFinite(totalAmount)
        ? (animation.target === totalAmount ? animation.interest : 0)
        : 1
    const displayedTotal = Number.isFinite(totalAmount)
        ? formatKrValue(totalAmount * balanceProgress)
        : totalValue
    const interestTypes = Array.from(
        new Set(accounts.flatMap((account) => (account.interestType ? [account.interestType] : [])))
    )

    useEffect(() => {
        if (!Number.isFinite(totalAmount)) {
            return
        }

        let animationFrameId = 0
        const duration = 450
        const startTime = performance.now()
        const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches

        function animate(currentTime: number) {
            const elapsedTime = reducedMotion ? duration * 2 : currentTime - startTime
            const balance = Math.min(elapsedTime / duration, 1)
            // Båda börjar samtidigt, men räntan tar dubbelt så lång tid.
            const interest = Math.min(elapsedTime / (duration * 2), 1)

            setAnimation({
                target: totalAmount,
                balance: 1 - Math.pow(1 - balance, 3),
                interest: 1 - Math.pow(1 - interest, 3),
            })

            if (interest < 1) {
                animationFrameId = requestAnimationFrame(animate)
            }
        }

        animationFrameId = requestAnimationFrame(animate)

        return () => cancelAnimationFrame(animationFrameId)
    }, [totalAmount])

    return (
        <div className="balance-overview">
            <div className="balance-card">
                <p className="balance-label">{totalLabel}</p>
                <p className="balance-value">{displayedTotal}</p>
                <p className="balance-sub">{subLabel}</p>
                {interestSummary?.(interestProgress)}
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
                        {account.interestRate && (
                            <p className="account-card-interest">
                                <span className="account-card-interest__rate">{account.interestRate} ränta</span>
                                {account.interestType && (
                                    <span className="account-card-interest__type">
                                        {account.interestType === 'FIXED' ? 'Fast' : 'Rörlig'}
                                    </span>
                                )}
                            </p>
                        )}
                    </button>
                ))}
            </div>

            {interestTypes.length > 0 && (
                <p className="accounts-interest-note">
                    <Info size={14} aria-hidden="true" />
                    <span>{interestTypes.map((type) => interestTypeExplanations[type]).join(' ')}</span>
                </p>
            )}
        </div>
    )
}
