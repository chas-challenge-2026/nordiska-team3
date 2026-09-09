export interface TransactAccount {
    id: string
    name: string
    balance: number
}

export const mockTransactAccounts: TransactAccount[] = [
    { id: 'education', name: 'Högskolesparande', balance: 45231 },
    { id: 'vacation', name: 'Semesterfonden', balance: 12800 },
    { id: 'buffer', name: 'Buffertsparande', balance: 78541 },
]