import { useMemo, useState } from 'react'
import { ChevronRight, Search } from 'lucide-react'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { UserProfile } from '../components/UserProfile'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { useTheme } from '../context/useTheme'
import { useLogout } from '../hooks/useLogout'
import './FaqPage.css'

const faqItems = [
    {
        id: 'open-account',
        question: 'Hur öppnar jag ett nytt sparkonto?',
        answer: 'Du öppnar ett nytt sparkonto från dashboarden genom att välja Skapa nytt sparkonto.',
    },
    {
        id: 'withdrawal-time',
        question: 'Hur lång tid tar ett uttag?',
        answer: 'Ett uttag hanteras normalt samma bankdag. I vissa fall kan det ta upp till nästa bankdag.',
    },
    {
        id: 'interest-statement',
        question: 'Var hittar jag mitt räntebesked?',
        answer: 'Du hittar räntebesked och underlag på sidan Skatterapport.',
    },
    {
        id: 'deposit-protection',
        question: 'Är mina pengar skyddade?',
        answer: 'Ja, dina pengar omfattas av insättningsgarantin enligt de villkor som gäller för kontot.',
    },
    {
        id: 'phone-number',
        question: 'Hur ändrar jag mitt telefonnummer?',
        answer: 'Du kan uppdatera dina kontaktuppgifter via profilknappen längst upp till höger.',
    },
    {
        id: 'multiple-accounts',
        question: 'Kan jag ha flera sparkonton?',
        answer: 'Ja, du kan skapa flera sparkonton och ge dem olika namn för olika sparmål.',
    },
]

function FaqPage() {
    const [searchTerm, setSearchTerm] = useState('')
    const [openQuestionId, setOpenQuestionId] = useState<string | null>(null)
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const visibleFaqItems = useMemo(() => {
        const normalizedSearch = searchTerm.trim().toLocaleLowerCase('sv-SE')

        if (!normalizedSearch) {
            return faqItems
        }

        return faqItems.filter((item) =>
            item.question.toLocaleLowerCase('sv-SE').includes(normalizedSearch)
        )
    }, [searchTerm])

    return (
        <main className="faq-page">
            <AppNav onLogout={handleLogout} onThemeToggle={toggleTheme} />

            <div aria-hidden="true">
                <DecorativeCircle color="orange" size={170} top={96} left="16%" opacity={0.94} />
                <DecorativeCircle color="green" size={126} top={124} right="11%" opacity={0.72} />
                <DecorativeCircle color="white" size={92} top={204} right="19%" opacity={0.9} />
                <DecorativeCircle color="blue" size={140} bottom={82} left="21%" opacity={0.78} />
            </div>

            <section className="faq-main">
                <UserProfile />

                <div className="faq-brand-circle brand-logo" aria-hidden="true">
                    <span>Sparportal</span>
                    <strong>nordiska<span className="faq-brand-dot">.</span></strong>
                </div>

                <div className="faq-content">
                    <header className="faq-header">
                        <h1>Vanliga frågor</h1>
                        <p>Sök bland vanliga frågor eller kontakta kundservice om du behöver mer hjälp.</p>
                    </header>

                    <label className="faq-search">
                        <span className="faq-sr-only">Sök bland vanliga frågor</span>
                        <Search size={16} aria-hidden="true" />
                        <input
                            type="search"
                            placeholder="Sök i frågorna..."
                            value={searchTerm}
                            onChange={(event) => setSearchTerm(event.target.value)}
                        />
                    </label>

                    {visibleFaqItems.length > 0 ? (
                        <div className="faq-list" aria-live="polite">
                            {visibleFaqItems.map((item) => (
                                <article className="faq-accordion" key={item.id}>
                                    <button
                                        className="faq-item"
                                        type="button"
                                        aria-expanded={openQuestionId === item.id}
                                        aria-controls={`faq-answer-${item.id}`}
                                        id={`faq-question-${item.id}`}
                                        onClick={() =>
                                            setOpenQuestionId((currentId) => currentId === item.id ? null : item.id)
                                        }
                                    >
                                        <span>{item.question}</span>
                                        <ChevronRight size={18} aria-hidden="true" />
                                    </button>

                                    <div
                                        className="faq-answer"
                                        id={`faq-answer-${item.id}`}
                                        role="region"
                                        aria-labelledby={`faq-question-${item.id}`}
                                        hidden={openQuestionId !== item.id}
                                    >
                                        <p>{item.answer}</p>
                                    </div>
                                </article>
                            ))}
                        </div>
                    ) : (
                        <section className="faq-no-results" aria-live="polite">
                            <h2>Inget svar hittades</h2>
                            <p>
                                Vi hittade ingen fråga som matchar din sökning. Testa att formulera om
                                eller använd kontaktvägarna längre ner på sidan.
                            </p>
                        </section>
                    )}
                </div>

                <CustomerServiceFooter />
            </section>
        </main>
    )
}

export default FaqPage
