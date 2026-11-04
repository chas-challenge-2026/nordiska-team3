import { useQuery } from '@tanstack/react-query'
import { getAccountsWithTransactions } from '../services/accountOverviewService'

export function useAccountsWithTransactions(page = 1, pageSize = 20) {
    return useQuery({
        queryKey: ['accountsWithTransactions', page, pageSize],
        queryFn: () => getAccountsWithTransactions(page, pageSize),
        staleTime: 30_000,
    })
}