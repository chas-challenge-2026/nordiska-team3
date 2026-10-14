import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { createTaxReport, getTaxReports } from '../services/taxReportService'
import type { TaxReport } from '../services/taxReportService'

function isInProgress(report: TaxReport) {
    return report.status === 'QUEUED' || report.status === 'PROCESSING'
}

/** Hämtar skatterapporter och uppdaterar medan en rapport skapas. */
export function useTaxReports() {
    return useQuery({
        queryKey: ['taxReports'],
        queryFn: getTaxReports,
        retry: 1,
        // Rapporter genereras i bakgrunden, så listan hämtas om tills alla är klara eller misslyckade
        refetchInterval: (query) => (query.state.data?.some(isInProgress) ? 3000 : false),
    })
}

/** Skapar en skatterapport och uppdaterar listan. */
export function useCreateTaxReport() {
    const queryClient = useQueryClient()

    return useMutation({
        mutationFn: createTaxReport,
        onSuccess: () => queryClient.invalidateQueries({ queryKey: ['taxReports'] }),
    })
}
