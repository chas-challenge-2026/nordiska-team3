import type { TaxReportYear } from '../Page/mockTaxReport'

// Mock: skapar en textfil-nedladdning tills backend har en riktig PDF-endpoint.
export function downloadTaxReport(report: TaxReportYear) {
    if (report.status !== 'ready') return

    const content = `Skatterapport ${report.year}\n\nTotala ränteintäkter: ${report.totalInterest} kr\nSkattesats: ${report.taxRate * 100}%\n\nUppdelning per konto:\n${report.accounts
        .map((a) => `- ${a.name}: ${a.interest} kr`)
        .join('\n')}\n`

    const blob = new Blob([content], { type: 'text/plain;charset=utf-8' })
    const url = URL.createObjectURL(blob)

    const link = document.createElement('a')
    link.href = url
    link.download = `skatterapport-${report.year}.txt`
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)
}