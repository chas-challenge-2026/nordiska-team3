import { useId } from 'react'
import type { BackendAccount } from '../../services/accountService'
import './AccruedInterest.css'

interface AccruedInterestProps {
    accounts: BackendAccount[]
    animationProgress?: number
}

const currency = new Intl.NumberFormat('sv-SE', {
    style: 'currency',
    currency: 'SEK',
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
})

export function AccruedInterest({ accounts, animationProgress = 1 }: AccruedInterestProps) {
    const titleId = useId()
    const interestAccounts = accounts.filter((account) => account.accountType === 'SAVINGS')
    const amounts = interestAccounts.map((account) => {
        const raw = account.accruedInterest?.replace(/\s/g, '').replace(',', '.')
        const amount = raw ? Number(raw) : NaN
        return { account, amount: Number.isFinite(amount) ? amount : null }
    })
    // Ett ofullständigt underlag ska aldrig visas som en komplett totalsumma.
    const isUnavailable = amounts.some(({ amount }) => amount === null)
    const total = amounts.reduce((sum, { amount }) => sum + (amount ?? 0), 0)

    return (
        <section className="accrued-interest" aria-labelledby={titleId}>
            <h2 id={titleId} className="balance-label">Ditt sparande växer</h2>
            <p className="balance-value accrued-interest__amount">
                {isUnavailable ? '—' : `${total > 0 ? '+' : ''}${currency.format(total * animationProgress)}`}
            </p>
            <p className="accrued-interest__period">Intjänad ränta i år</p>
            <p className="accrued-interest__note">
                {isUnavailable ? 'Din upplupna ränta kan inte visas just nu.'
                        : interestAccounts.length === 0 ? 'När du börjar spara ser du din intjänade ränta här.'
                            : 'Före skatt · ännu inte utbetald'}
            </p>
        </section>
    )
}
