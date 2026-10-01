import { useQuery } from '@tanstack/react-query'
import { getAccountsWithTransactions } from '../services/accountOverviewService'

export function useAccountsWithTransactions() {
    return useQuery({
        queryKey: ['accountsWithTransactions'],
        queryFn: getAccountsWithTransactions,
        staleTime: 30_000,
    })
}