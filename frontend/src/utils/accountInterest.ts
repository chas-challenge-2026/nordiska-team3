import type { BackendAccount } from '../services/accountService'

export type InterestType = 'VARIABLE' | 'FIXED'

type AccountInterest = {
    rate: number
    type: InterestType
}

const interestTypeLabels: Record<InterestType, string> = {
    VARIABLE: 'rörlig',
    FIXED: 'fast',
}

export const interestTypeExplanations: Record<InterestType, string> = {
    VARIABLE: 'Rörlig ränta kan ändras om marknadsräntan ändras.',
    FIXED: 'Fast ränta är densamma under hela bindningstiden.',
}

/** Parses backend money/rate strings that may use either comma or dot decimals. */
function parseAmount(value: string) {
    return Number(value.replace(/\s/g, '').replace(',', '.'))
}

/** Returns account interest only when backend supplies both rate and rate type. */
export function getAccountInterest(account: BackendAccount): AccountInterest | null {
    if (account.interestRate === undefined || !account.interestType) {
        return null
    }

    const rate = parseAmount(account.interestRate)

    return Number.isNaN(rate) ? null : { rate, type: account.interestType }
}

/** Formats accrued interest from backend, or returns null when it is unavailable. */
export function formatAccruedInterest(account: BackendAccount) {
    if (account.accruedInterest === undefined) {
        return null
    }

    const amount = parseAmount(account.accruedInterest)

    if (Number.isNaN(amount)) {
        return null
    }

    return `${amount.toLocaleString('sv-SE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} kr`
}

/** Formats account interest for compact account cards, e.g. "3,50 % · rörlig". */
export function formatAccountInterest({ rate, type }: AccountInterest) {
    const formattedRate = rate.toLocaleString('sv-SE', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })

    return `${formattedRate} % · ${interestTypeLabels[type]}`
}
