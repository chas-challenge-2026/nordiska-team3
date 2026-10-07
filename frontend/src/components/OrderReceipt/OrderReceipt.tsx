import { Link } from 'react-router-dom'
import './OrderReceipt.css'

export type OrderReceiptData = {
    type: 'deposit' | 'withdrawal' | 'transfer'
    amount: number
    accountName: string
    // Bara vid överföring: kontot pengarna flyttades till
    toAccountName?: string
    // Nytt saldo på accountName (från-kontot vid överföring), direkt från API-svaret
    balance: number
}

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE', { maximumFractionDigits: 2 })} kr`
}

function getHeadline({ type, amount, accountName, toAccountName }: OrderReceiptData) {
    // "finns nu på" säger samtidigt att pengarna finns där direkt (backend genomför allt direkt)
    if (type === 'deposit') return `${formatKr(amount)} finns nu på ${accountName}.`
    if (type === 'transfer') return `${formatKr(amount)} finns nu på ${toAccountName}.`

    return `${formatKr(amount)} har tagits ut från ${accountName}.`
}

function getBalanceText({ type, accountName, balance }: OrderReceiptData) {
    return type === 'transfer' ? `Kvar på ${accountName}: ${formatKr(balance)}` : `Nytt saldo ${formatKr(balance)}`
}

// Kvitto efter genomförd order. Två rader som får plats i den reserverade meddelandeytan;
// mycket långa kontonamn kortas med "…" i stället för att layouten hoppar.
export function OrderReceipt({ receipt }: { receipt: OrderReceiptData }) {
    const headline = getHeadline(receipt)
    const balanceText = getBalanceText(receipt)

    return (
        <span className="order-receipt">
            <strong className="order-receipt__line" title={headline}>
                {headline}
            </strong>
            <span className="order-receipt__line">
                <span title={balanceText}>{balanceText}</span>
                <span aria-hidden="true"> · </span>
                <Link className="order-receipt__link" to="/historik">
                    Visa historik
                </Link>
            </span>
        </span>
    )
}
