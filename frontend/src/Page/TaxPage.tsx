import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import './TaxPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { useTheme } from '../context/useTheme'
import { mockTaxReports, type TaxReportStatus } from './mockTaxReport'
import { Download, Loader2, CheckCircle2, XCircle } from 'lucide-react'

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} kr`
}

function TaxPage() {
    const navigate = useNavigate()
    const { toggleTheme } = useTheme()

    const [reports, setReports] = useState(mockTaxReports)
    const [selectedYear, setSelectedYear] = useState(mockTaxReports[0].year)

    const report = reports.find((r) => r.year === selectedYear)!
    const calculatedTax = report.totalInterest * report.taxRate

    function handleLogout() {
        navigate('/login')
    }

    function handleGenerate() {
        setReports((prev) =>
            prev.map((r) => (r.year === selectedYear ? { ...r, status: 'pending' as TaxReportStatus } : r))
        )

        setTimeout(() => {
            setReports((prev) =>
                prev.map((r) => (r.year === selectedYear ? { ...r, status: 'ready' as TaxReportStatus } : r))
            )
        }, 1500)
    }

    function renderActionButton() {
        if (report.status === 'pending') {
            return (
                <button type="button" className="tax-download-btn" disabled>
                    <Loader2 size={18} className="tax-spin" />
                    Genererar rapport...
                </button>
            )
        }

        if (report.status === 'failed') {
            return (
                <button type="button" className="tax-download-btn tax-download-btn--retry" onClick={handleGenerate}>
                    <XCircle size={18} />
                    Misslyckades – försök igen
                </button>
            )
        }

        return (
            <button type="button" className="tax-download-btn" onClick={handleGenerate}>
                <Download size={18} />
                Ladda ner PDF-rapport
            </button>
        )
    }

    return (
        <div className="dashboard-page">
            <DecorativeCircle color="orange" size={70} left={430} top={210} />
            <DecorativeCircle color="blue" size={110} right={330} top={170} />
            <DecorativeCircle color="green" size={150} right={200} top={140} opacity={0.95} />
            <DecorativeCircle color="blue" size={110} left={440} bottom={140} />
            <DecorativeCircle color="orange" size={150} left={500} bottom={80} />

            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <div className="tax-brand-circle">
                <span>Sparportal</span>
                <strong>nordiska<span className="tax-brand-dot">.</span></strong>
            </div>

            <main className="dashboard-main">
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
                                {r.year}
                            </button>
                        ))}
                    </div>

                    <div className="tax-status-row">
                        <span className={`tax-status-badge tax-status-badge--${report.status}`}>
                            {report.status === 'pending' && 'Genereras'}
                            {report.status === 'ready' && (
                                <>
                                    <CheckCircle2 size={12} /> Klar
                                </>
                            )}
                            {report.status === 'failed' && 'Misslyckad'}
                        </span>
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

                    {renderActionButton()}
                </div>
            </main>
        </div>
    )
}

export default TaxPage