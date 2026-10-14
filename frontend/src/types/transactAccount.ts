import type { ReactNode } from 'react'
import type { AccountVariant } from '../utils/accountPresentation'

export interface TransactAccount {
    id: string
    accountNumber: string
    name: string
    balance: number
    icon?: ReactNode
    variant?: AccountVariant
}
