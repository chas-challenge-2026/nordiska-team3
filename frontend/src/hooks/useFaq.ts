import { useQuery } from '@tanstack/react-query'
import { getFaqEntries, searchFaq } from '../services/faqService'

// FAQ ändras sällan, så listan hålls i cache en stund
export function useFaqEntries() {
    return useQuery({
        queryKey: ['faq'],
        queryFn: getFaqEntries,
        staleTime: 5 * 60_000,
        // Ett nytt försök räcker, så att felmeddelandet inte dröjer ~7 sekunder
        retry: 1,
    })
}

// Backendens smarta sökning (synonymer, stavfel). Körs bara när enabled är true,
// t.ex. när den vanliga filtreringen i listan inte hittar något.
export function useFaqSearch(query: string, enabled: boolean) {
    return useQuery({
        queryKey: ['faqSearch', query],
        queryFn: () => searchFaq(query),
        enabled: enabled && query.length > 0,
        staleTime: 5 * 60_000,
    })
}
