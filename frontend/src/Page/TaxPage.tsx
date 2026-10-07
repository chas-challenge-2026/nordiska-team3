import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import './TaxPage.css'
import { AppNav } from '../components/AppNav'
import { UserProfile } from '../components/UserProfile'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { useTheme } from '../context/useTheme'
import { mockTaxReports } from './mockTaxReport'
import type { TaxReportStatus, TaxReportYear } from './mockTaxReport'
import { ChevronLeft, ChevronRight, Download, Loader2, RotateCcw, Plus } from 'lucide-react'
import { downloadTaxReport } from '../services/taxReportService'

const statusLabel: Record<TaxReportStatus, string> = {
    pending: 'Genereras',
    ready: 'Klar',
    failed: 'Misslyckad',
}

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} kr`
}

function TaxPage() {
    const navigate = useNavigate()
    const { toggleTheme } = useTheme()

    const [reports, setReports] = useState<TaxReportYear[]>(mockTaxReports)
    const [selectedYear, setSelectedYear] = useState(mockTaxReports[0].year)
    const [reportHistoryPage, setReportHistoryPage] = useState(1)
    const reportHistoryPageSize = 5

    const report = reports.find((r) => r.year === selectedYear)!
    const calculatedTax = report.totalInterest * report.taxRate

    const latestYear = Math.max(...reports.map((r) => r.year))
    const nextAvailableYear = latestYear + 1
    const hasNextYearReport = reports.some((r) => r.year === nextAvailableYear)
    const reportHistoryTotalPages = Math.max(1, Math.ceil(reports.length / reportHistoryPageSize))
    const paginatedReports = reports.slice(
        (reportHistoryPage - 1) * reportHistoryPageSize,
        reportHistoryPage * reportHistoryPageSize
    )

    function handleLogout() {
        navigate('/login')
    }

    function handleRequestNewReport() {
        const newReport: TaxReportYear = {
            year: nextAvailableYear,
            status: 'pending',
            totalInterest: 0,
            taxRate: 0.3,
            accounts: [],
        }

        setReports((prev) => [newReport, ...prev])
        setSelectedYear(nextAvailableYear)
        setReportHistoryPage(1)

        // Mock: simulerar generering tills backend har en riktig endpoint.
        setTimeout(() => {
            setReports((prev) =>
                prev.map((r) =>
                    r.year === nextAvailableYear
                        ? {
                              ...r,
                              status: 'ready',
                              totalInterest: 340.2,
                              accounts: [
                                  { id: '1', name: 'Högskolesparande', interest: 80.1, color: 'green' },
                                  { id: '2', name: 'Semesterfonden', interest: 45.6, color: 'orange' },
                                  { id: '3', name: 'Buffertsparande', interest: 214.5, color: 'purple' },
                              ],
                          }
                        : r
                )
            )
        }, 2000)
    }

    function handleRetry(year: number) {
        setReports((prev) =>
            prev.map((r) => (r.year === year ? { ...r, status: 'pending' } : r))
        )

        setTimeout(() => {
            setReports((prev) =>
                prev.map((r) => (r.year === year ? { ...r, status: 'ready' } : r))
            )
        }, 1500)
    }

    function goToPreviousReportPage() {
        setReportHistoryPage((page) => Math.max(1, page - 1))
    }

    function goToNextReportPage() {
        setReportHistoryPage((page) => Math.min(reportHistoryTotalPages, page + 1))
    }

    function renderActionButton(r: TaxReportYear) {
        if (r.status === 'pending') {
            return (
                <button type="button" className="tax-history-download" disabled aria-label="Genererar rapport">
                    <Loader2 size={16} className="tax-spin" />
                </button>
            )
        }

        if (r.status === 'failed') {
            return (
                <button
                    type="button"
                    className="tax-history-download"
                    onClick={() => handleRetry(r.year)}
                    aria-label={`Försök igen för rapport ${r.year}`}
                >
                    <RotateCcw size={16} />
                </button>
            )
        }

        return (
            <button
                type="button"
                className="tax-history-download"
                onClick={() => downloadTaxReport(r)}
                aria-label={`Ladda ner rapport ${r.year}`}
            >
                <Download size={16} />
            </button>
        )
    }

    return (
        <div className="dashboard-page">
            <DecorativeCircle color="orange" size={150} left={-30} top={180} />
            <DecorativeCircle color="blue" size={95} left={40} top={320} opacity={0.9} />
            <DecorativeCircle color="green" size={150} right={-25} top={145} opacity={0.95} />
            <DecorativeCircle color="blue" size={115} right={70} bottom={150} />
            <DecorativeCircle color="orange" size={155} left={190} bottom={70} />

            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <div className="tax-brand-circle brand-logo">
                <span>Sparportal</span>
                <strong>nordiska<span className="tax-brand-dot">.</span></strong>
            </div>

            <main className="dashboard-main">
                <UserProfile />

                <div className="tax-content">
                    <div className="tax-header">
                        <h1>Skatterapport</h1>
                        <p>Sammanställning av ränteintäkter för deklaration.</p>
                    </div>

                    <div className="pill-toggle-row">
                        {reports.map((r) => (
                            <button
                                key={r.year}
                                type="button"
                                className={`pill-toggle ${selectedYear === r.year ? 'pill-toggle--active' : ''}`}
                                onClick={() => setSelectedYear(r.year)}
                            >
                                <span className={`pill-status-dot pill-status-dot--${r.status}`} />
                                {r.year}
                            </button>
                        ))}
                    </div>

                    <div className="tax-summary-row">
                        <div className="tax-summary-card">
                            <p className="tax-summary-label">TOTALA RÄNTEINTÄKTER</p>
                            <p className="tax-summary-value tax-summary-value--green">
                                {formatKr(report.totalInterest)}
                            </p>
                        </div>
                        <div className="tax-summary-card">
                            <p className="tax-summary-label">BERÄKNAD SKATT ({Math.round(report.taxRate * 100)}%)</p>
                            <p className="tax-summary-value">{formatKr(calculatedTax)}</p>
                        </div>
                    </div>

                    <div className="tax-breakdown-card">
                        <p className="tax-breakdown-title">UPPDELNING PER KONTO</p>
                        {report.accounts.map((acc) => (
                            <div className="tax-breakdown-row" key={acc.id}>
                                <div className="tax-breakdown-name">
                                    <span className={`tax-dot tax-dot--${acc.color}`} />
                                    {acc.name}
                                </div>
                                <span className={`tax-breakdown-amount tax-breakdown-amount--${acc.color}`}>
                                    {formatKr(acc.interest)}
                                </span>
                            </div>
                        ))}
                    </div>

                    <div className="tax-history-card">
                        <p className="tax-breakdown-title">RAPPORTHISTORIK</p>
                        {paginatedReports.map((r) => (
                            <div className="tax-history-row" key={r.year}>
                                <div className="tax-history-year">
                                    <span>{r.year}</span>
                                    <span className={`tax-status-badge tax-status-badge--${r.status}`}>
                                        {statusLabel[r.status]}
                                    </span>
                                </div>
                                {renderActionButton(r)}
                            </div>
                        ))}

                        {reportHistoryTotalPages > 1 && (
                            <nav className="tax-pagination" aria-label="Sidnavigering för rapporthistorik">
                                <button
                                    type="button"
                                    className="tax-pagination__button"
                                    onClick={goToPreviousReportPage}
                                    disabled={reportHistoryPage === 1}
                                    aria-label="Visa föregående rapportsida"
                                >
                                    <ChevronLeft size={16} />
                                </button>
                                <span className="tax-pagination__status">
                                    Sida {reportHistoryPage} av {reportHistoryTotalPages}
                                </span>
                                <button
                                    type="button"
                                    className="tax-pagination__button"
                                    onClick={goToNextReportPage}
                                    disabled={reportHistoryPage === reportHistoryTotalPages}
                                    aria-label="Visa nästa rapportsida"
                                >
                                    <ChevronRight size={16} />
                                </button>
                            </nav>
                        )}
                    </div>

                    {!hasNextYearReport && (
                        <button type="button" className="tax-request-btn" onClick={handleRequestNewReport}>
                            <Plus size={16} />
                            Begär rapport för {nextAvailableYear}
                        </button>
                    )}
                </div>

                <CustomerServiceFooter />
            </main>
        </div>
    )
}

export default TaxPage
