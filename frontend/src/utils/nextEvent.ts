import type { AccountWithTransactions } from '../services/accountOverviewService'
import { getAccountInterest } from './accountInterest'

export type NextEventKind = 'transaction' | 'interest'

export type NextEvent = {
    kind: NextEventKind
    title: string
    description: string
    // Rubrik ovanför datumet, t.ex. "Beräknas klart"
    dateLabel: string
    // Null när datumet inte är känt (t.ex. pågående transaktion utan förväntat datum)
    date: Date | null
}

const pendingTitles: Record<string, string> = {
    DEPOSIT: 'Insättning pågår',
    WITHDRAWAL: 'Uttag pågår',
    TRANSFER: 'Överföring pågår',
}

function startOfDay(date: Date) {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate())
}

// "2026-10-07" tolkas som lokal dag (new Date() skulle läsa det som UTC)
function parseDay(value: string) {
    const match = /^(\d{4})-(\d{2})-(\d{2})$/.exec(value)

    return match
        ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3]))
        : startOfDay(new Date(value))
}

function formatKr(amount: string) {
    const value = Math.abs(Number(amount.replace(/\s/g, '').replace(',', '.')))

    return `${value.toLocaleString('sv-SE', { maximumFractionDigits: 2 })} kr`
}

// Tillfälligt: backend skickar inte nextInterestPayoutDate än, så vi antar
// att räntan betalas ut vid årsskiftet (31 december) för konton med ränta.
function getInterestPayoutDate(account: AccountWithTransactions['account'], today: Date) {
    if (account.nextInterestPayoutDate) {
        return parseDay(account.nextInterestPayoutDate)
    }

    return new Date(today.getFullYear(), 11, 31)
}

function getPendingTransactionEvent(groups: AccountWithTransactions[]): NextEvent | null {
    const pending = groups
        .flatMap(({ account, history }) =>
            history.transactions
                .filter((transaction) => transaction.status === 'PENDING')
                .map((transaction) => ({
                    account,
                    transaction,
                    date: transaction.expectedCompletionDate
                        ? parseDay(transaction.expectedCompletionDate)
                        : null,
                }))
        )
        // Känt datum först, närmast först. Okänt datum sist.
        .sort((a, b) => (a.date?.getTime() ?? Infinity) - (b.date?.getTime() ?? Infinity))

    const first = pending[0]

    if (!first) {
        return null
    }

    const others = pending.length - 1

    return {
        kind: 'transaction',
        title: pendingTitles[first.transaction.transactionType] ?? 'Transaktion pågår',
        description:
            `${formatKr(first.transaction.amount)} · ${first.account.name}` +
            (others > 0 ? ` · ${others} till pågår` : ''),
        dateLabel: first.date ? 'Beräknas klart' : 'Status',
        date: first.date,
    }
}

function getInterestPayoutEvent(groups: AccountWithTransactions[], today: Date): NextEvent | null {
    const payouts = groups
        .filter(({ account }) => (getAccountInterest(account)?.rate ?? 0) > 0)
        .map(({ account }) => ({ account, date: getInterestPayoutDate(account, today) }))
        .filter(({ date }) => date >= today)
        .sort((a, b) => a.date.getTime() - b.date.getTime())

    const first = payouts[0]

    if (!first) {
        return null
    }

    const sameDay = payouts.filter(({ date }) => date.getTime() === first.date.getTime())

    return {
        kind: 'interest',
        title: 'Ränteutbetalning',
        description:
            sameDay.length === 1
                ? `Räntan på ${first.account.name} sätts in på kontot.`
                : `Räntan på ${sameDay.length} konton sätts in på respektive konto.`,
        dateLabel: 'Datum',
        date: first.date,
    }
}

// Pågående transaktioner går före ränteutbetalningar: de är det kunden
// oftast undrar över ("var är mina pengar?").
export function getNextEvent(groups: AccountWithTransactions[], now = new Date()): NextEvent | null {
    const today = startOfDay(now)

    return getPendingTransactionEvent(groups) ?? getInterestPayoutEvent(groups, today)
}

// "i dag", "i morgon", "torsdag 31 december" (år läggs till om det inte är i år)
export function formatEventDate(date: Date, now = new Date()) {
    const today = startOfDay(now)
    const days = Math.round((startOfDay(date).getTime() - today.getTime()) / 86_400_000)

    if (days === 0) return 'i dag'
    if (days === 1) return 'i morgon'

    return new Intl.DateTimeFormat('sv-SE', {
        weekday: 'long',
        day: 'numeric',
        month: 'long',
        ...(date.getFullYear() !== today.getFullYear() ? { year: 'numeric' } : {}),
    }).format(date)
}
