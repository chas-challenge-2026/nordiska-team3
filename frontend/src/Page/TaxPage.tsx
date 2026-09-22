import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import './TaxPage.css'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { useTheme } from '../context/useTheme'
import { mockTaxReports } from './mockTaxReport'
import { Download } from 'lucide-react'

function formatKr(amount: number) {
    return `${amount.toLocaleString('sv-SE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} kr`
}

function TaxPage() {
    const navigate = useNavigate()
    const { toggleTheme } = useTheme()

    const [selectedYear, setSelectedYear] = useState(mockTaxReports[0].year)
    const [isDownloading, setIsDownloading] = useState(false)

    const report = mockTaxReports.find((r) => r.year === selectedYear)!
    const calculatedTax = report.totalInterest * report.taxRate

    function handleLogout() {
        navigate('/login')
    }

    function handleDownload() {
        setIsDownloading(true)
        // Mock: simulerar nedladdning tills backend finns
        setTimeout(() => setIsDownloading(false), 1000)
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
                        {mockTaxReports.map((r) => (
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

                    <button
                        type="button"
                        className="tax-download-btn"
                        onClick={handleDownload}
                        disabled={isDownloading}
                    >
                        <Download size={18} />
                        {isDownloading ? 'Förbereder rapport...' : 'Ladda ner PDF-rapport'}
                    </button>
                </div>
            </main>
        </div>
    )
}

export default TaxPage