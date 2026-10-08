import { useState } from 'react'
import './TaxPage.css'
import { AppNav } from '../components/AppNav'
import { UserProfile } from '../components/UserProfile'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { useTheme } from '../context/useTheme'
import { useLogout } from '../hooks/useLogout'
import { useCreateTaxReport, useTaxReports } from '../hooks/useTaxReports'
import { downloadTaxReport } from '../services/taxReportService'
import type { TaxReport, TaxReportStatus } from '../services/taxReportService'
import { ChevronLeft, ChevronRight, Download, Loader2, RotateCcw, Plus } from 'lucide-react'

const statusLabel: Record<TaxReportStatus, string> = {
    QUEUED: 'I kö',
    PROCESSING: 'Genereras',
    READY: 'Klar',
    FAILED: 'Misslyckad',
}

// CSS-klasserna för status är skrivna med små bokstäver (pending/ready/failed)
const statusClass: Record<TaxReportStatus, string> = {
    QUEUED: 'pending',
    PROCESSING: 'pending',
    READY: 'ready',
    FAILED: 'failed',
}

// Första året backend har data för. Kunden kan begära detta år och fram till innevarande år.
const FIRST_REPORT_YEAR = 2025

function isInProgress(report: TaxReport) {
    return report.status === 'QUEUED' || report.status === 'PROCESSING'
}

function formatDate(value: string | null) {
    if (!value) return '–'

    return new Date(value).toLocaleString('sv-SE', { dateStyle: 'medium', timeStyle: 'short' })
}

// Backend skickar alla försök, nyaste först inom varje år. Sidan visar bara det senaste per år.
function latestReportPerYear(reports: TaxReport[]) {
    const seenYears = new Set<number>()

    return reports.filter((r) => {
        if (seenYears.has(r.reportYear)) return false
        seenYears.add(r.reportYear)
        return true
    })
}

function TaxPage() {
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const { data = [], isLoading, isError, refetch } = useTaxReports()
    const createReport = useCreateTaxReport()
    const [selectedYear, setSelectedYear] = useState<number | null>(null)
    const [reportHistoryPage, setReportHistoryPage] = useState(1)
    const [downloadError, setDownloadError] = useState<string | null>(null)
    const reportHistoryPageSize = 5

    const reports = latestReportPerYear(data)
    const report = reports.find((r) => r.reportYear === selectedYear) ?? reports[0]

    const currentYear = new Date().getFullYear()
    const requestableYears = Array.from(
        { length: Math.max(0, currentYear - FIRST_REPORT_YEAR + 1) },
        (_, i) => currentYear - i
    )
    const yearsWithoutReport = requestableYears.filter((year) => !reports.some((r) => r.reportYear === year))

    const reportHistoryTotalPages = Math.max(1, Math.ceil(reports.length / reportHistoryPageSize))
    const paginatedReports = reports.slice(
        (reportHistoryPage - 1) * reportHistoryPageSize,
        reportHistoryPage * reportHistoryPageSize
    )

    function requestReport(year: number) {
        createReport.mutate(year, {
            onSuccess: () => {
                setSelectedYear(year)
                setReportHistoryPage(1)
            },
        })
    }

    async function handleDownload(r: TaxReport) {
        setDownloadError(null)

        try {
            await downloadTaxReport(r)
        } catch (error) {
            setDownloadError(error instanceof Error ? error.message : 'Rapporten kunde inte laddas ner.')
        }
    }

    function goToPreviousReportPage() {
        setReportHistoryPage((page) => Math.max(1, page - 1))
    }

    function goToNextReportPage() {
        setReportHistoryPage((page) => Math.min(reportHistoryTotalPages, page + 1))
    }

    function renderActionButton(r: TaxReport) {
        if (isInProgress(r)) {
            return (
                <button type="button" className="tax-history-download" disabled aria-label="Genererar rapport">
                    <Loader2 size={16} className="tax-spin" />
                </button>
            )
        }

        if (r.status === 'FAILED') {
            return (
                <button
                    type="button"
                    className="tax-history-download"
                    onClick={() => requestReport(r.reportYear)}
                    disabled={createReport.isPending}
                    aria-label={`Försök igen för rapport ${r.reportYear}`}
                >
                    <RotateCcw size={16} />
                </button>
            )
        }

        return (
            <button
                type="button"
                className="tax-history-download"
                onClick={() => handleDownload(r)}
                disabled={!r.pdfAvailable}
                aria-label={
                    r.pdfAvailable
                        ? `Ladda ner rapport ${r.reportYear}`
                        : `Filen för rapport ${r.reportYear} finns inte längre`
                }
            >
                <Download size={16} />
            </button>
        )
    }

    function renderContent() {
        if (isLoading) {
            return (
                <div className="tax-message-card" aria-live="polite">
                    <h2>Hämtar skatterapporter …</h2>
                </div>
            )
        }

        if (isError) {
            return (
                <div className="tax-message-card" role="alert">
                    <h2>Skatterapporterna kunde inte hämtas</h2>
                    <p>Försök igen om en stund, eller kontakta kundservice längre ner på sidan.</p>
                    <button className="tax-retry" type="button" onClick={() => refetch()}>
                        Försök igen
                    </button>
                </div>
            )
        }

        if (!report) {
            return (
                <div className="tax-message-card">
                    <h2>Inga skatterapporter än</h2>
                    <p>Begär en rapport nedan så sammanställer vi dina ränteintäkter för deklarationen.</p>
                </div>
            )
        }

        return (
            <>
                <div className="pill-toggle-row">
                    {reports.map((r) => (
                        <button
                            key={r.reportYear}
                            type="button"
                            className={`pill-toggle ${report.reportYear === r.reportYear ? 'pill-toggle--active' : ''}`}
                            onClick={() => setSelectedYear(r.reportYear)}
                            aria-pressed={report.reportYear === r.reportYear}
                        >
                            <span className={`pill-status-dot pill-status-dot--${statusClass[r.status]}`} />
                            {r.reportYear}
                        </button>
                    ))}
                </div>

                <div className="tax-breakdown-card" aria-live="polite">
                    <p className="tax-breakdown-title">RAPPORT {report.reportYear}</p>
                    <div className="tax-breakdown-row">
                        <span className="tax-breakdown-name">Status</span>
                        <span className={`tax-status-badge tax-status-badge--${statusClass[report.status]}`}>
                            {statusLabel[report.status]}
                        </span>
                    </div>
                    <div className="tax-breakdown-row">
                        <span className="tax-breakdown-name">Begärd</span>
                        <span className="tax-breakdown-value">{formatDate(report.createdAt)}</span>
                    </div>
                    <div className="tax-breakdown-row">
                        <span className="tax-breakdown-name">Klar</span>
                        <span className="tax-breakdown-value">{formatDate(report.completedAt)}</span>
                    </div>
                    {report.status === 'READY' && (
                        <div className="tax-breakdown-row">
                            <span className="tax-breakdown-name">Signerad</span>
                            <span className="tax-breakdown-value">{report.signed ? 'Ja' : 'Nej'}</span>
                        </div>
                    )}
                    {report.status === 'FAILED' && (
                        <p className="tax-report-note tax-report-note--error">
                            {report.errorMessage ?? 'Rapporten kunde inte skapas.'}
                        </p>
                    )}
                    {report.status === 'READY' && !report.pdfAvailable && (
                        <p className="tax-report-note">Filen finns inte längre. Begär en ny rapport nedan.</p>
                    )}
                    {/* Varje POST skapar en ny rapport, så knappen visas bara när ingen rapport för året genereras */}
                    {!isInProgress(report) && (
                        <button
                            type="button"
                            className="tax-retry"
                            onClick={() => requestReport(report.reportYear)}
                            disabled={createReport.isPending}
                        >
                            Begär ny rapport för {report.reportYear}
                        </button>
                    )}
                </div>

                {report.status === 'READY' && report.pdfAvailable && (
                    <button type="button" className="tax-download-btn" onClick={() => handleDownload(report)}>
                        <Download size={16} />
                        Ladda ner PDF
                    </button>
                )}

                {downloadError && (
                    <p className="tax-report-note tax-report-note--error tax-report-note--on-dark" role="alert">
                        {downloadError}
                    </p>
                )}

                <div className="tax-history-card">
                    <p className="tax-breakdown-title">RAPPORTHISTORIK</p>
                    {paginatedReports.map((r) => (
                        <div className="tax-history-row" key={r.id}>
                            <div className="tax-history-year">
                                <span>{r.reportYear}</span>
                                <span className={`tax-status-badge tax-status-badge--${statusClass[r.status]}`}>
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
            </>
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

                    {renderContent()}

                    {!isLoading &&
                        !isError &&
                        yearsWithoutReport.map((year) => (
                            <button
                                key={year}
                                type="button"
                                className="tax-request-btn"
                                onClick={() => requestReport(year)}
                                disabled={createReport.isPending}
                            >
                                {createReport.isPending && createReport.variables === year ? (
                                    <Loader2 size={16} className="tax-spin" />
                                ) : (
                                    <Plus size={16} />
                                )}
                                Begär rapport för {year}
                            </button>
                        ))}

                    {createReport.isError && (
                        <p className="tax-report-note tax-report-note--error tax-report-note--on-dark" role="alert">
                            {createReport.error.message}
                        </p>
                    )}
                </div>

                <CustomerServiceFooter />
            </main>
        </div>
    )
}

export default TaxPage
