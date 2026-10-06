import type { BackendAccount } from '../services/accountService'

export type InterestType = 'VARIABLE' | 'FIXED'

type AccountInterest = {
    rate: number
    type: InterestType
}

// Tillfälliga värden tills backend skickar interestRate/interestType på kontot.
// Sparkontots ränta är samma som i FAQ:n.
const fallbackInterestByAccountType: Record<string, AccountInterest> = {
    SAVINGS: { rate: 3.5, type: 'VARIABLE' },
    CHECKING: { rate: 0, type: 'VARIABLE' },
}

const interestTypeLabels: Record<InterestType, string> = {
    VARIABLE: 'rörlig',
    FIXED: 'fast',
}

export const interestTypeExplanations: Record<InterestType, string> = {
    VARIABLE: 'Rörlig ränta kan ändras om marknadsräntan ändras.',
    FIXED: 'Fast ränta är densamma under hela bindningstiden.',
}

export function getAccountInterest(account: BackendAccount): AccountInterest | null {
    if (account.interestRate !== undefined && account.interestType) {
        const rate = Number(account.interestRate.replace(',', '.'))

        if (!Number.isNaN(rate)) {
            return { rate, type: account.interestType }
        }
    }

    return fallbackInterestByAccountType[account.accountType] ?? null
}

// Ex: "3,50 % · rörlig"
export function formatAccountInterest({ rate, type }: AccountInterest) {
    const formattedRate = rate.toLocaleString('sv-SE', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2,
    })

    return `${formattedRate} % · ${interestTypeLabels[type]}`
}
