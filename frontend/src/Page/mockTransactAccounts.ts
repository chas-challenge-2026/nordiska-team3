import type { ReactNode } from 'react'
import type { AccountVariant } from '../utils/accountPresentation'

export interface TransactAccount {
    id: string
    name: string
    balance: number
    icon?: ReactNode
    variant?: AccountVariant
}

export const mockTransactAccounts: TransactAccount[] = [
    { id: 'education', name: 'Högskolesparande', balance: 45231 },
    { id: 'vacation', name: 'Semesterfonden', balance: 12800 },
    { id: 'buffer', name: 'Buffertsparande', balance: 78541 },
]
