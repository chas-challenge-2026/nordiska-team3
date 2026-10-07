import { useEffect, useMemo, useState } from 'react'
import { ChevronRight, Search } from 'lucide-react'
import { AppNav } from '../components/AppNav'
import { DecorativeCircle } from '../components/DecorativeCircle'
import { UserProfile } from '../components/UserProfile'
import { CustomerServiceFooter } from '../components/CustomerServiceFooter'
import { useTheme } from '../context/useTheme'
import { useLogout } from '../hooks/useLogout'
import { useFaqEntries, useFaqSearch } from '../hooks/useFaq'
import type { FaqEntry } from '../services/faqService'
import './FaqPage.css'

function matchesSearch(item: FaqEntry, search: string) {
    return item.question.toLocaleLowerCase('sv-SE').includes(search)
}

function FaqPage() {
    const [searchTerm, setSearchTerm] = useState('')
    const [debouncedSearch, setDebouncedSearch] = useState('')
    const [openQuestionId, setOpenQuestionId] = useState<string | null>(null)
    const handleLogout = useLogout()
    const { toggleTheme } = useTheme()

    const { data: faqItems = [], isLoading, isError, refetch } = useFaqEntries()
    const normalizedSearch = searchTerm.trim().toLocaleLowerCase('sv-SE')

    const visibleFaqItems = useMemo(() => {
        if (!normalizedSearch) {
            return faqItems
        }

        return faqItems.filter((item) => matchesSearch(item, normalizedSearch))
    }, [faqItems, normalizedSearch])

    // Vänta tills användaren slutat skriva innan backend tillfrågas
    useEffect(() => {
        const timeout = setTimeout(() => setDebouncedSearch(searchTerm.trim()), 400)

        return () => clearTimeout(timeout)
    }, [searchTerm])

    // Hittar filtreringen inget används backendens smarta sökning, som förstår t.ex. "när kommer pengarna"
    const isTyping = debouncedSearch !== searchTerm.trim()
    const shouldUseSmartSearch =
        !isLoading && !isError && !isTyping && debouncedSearch.length > 0 && visibleFaqItems.length === 0
    const smartSearch = useFaqSearch(debouncedSearch, shouldUseSmartSearch)
    const isWaitingForSmartSearch =
        normalizedSearch.length > 0 &&
        visibleFaqItems.length === 0 &&
        (isTyping || smartSearch.isFetching)
    const bestMatch = smartSearch.data?.matchFound ? smartSearch.data : null

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
                        <h1 tabIndex={0}>Vanliga frågor</h1>
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

                    {isLoading ? (
                        <section className="faq-no-results" aria-live="polite">
                            <h2>Hämtar vanliga frågor …</h2>
                        </section>
                    ) : isError ? (
                        <section className="faq-no-results" role="alert">
                            <h2>Vanliga frågor kunde inte hämtas</h2>
                            <p>Försök igen om en stund, eller kontakta kundservice längre ner på sidan.</p>
                            <button className="faq-retry" type="button" onClick={() => refetch()}>
                                Försök igen
                            </button>
                        </section>
                    ) : visibleFaqItems.length > 0 ? (
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
                    ) : isWaitingForSmartSearch ? (
                        <section className="faq-no-results" aria-live="polite">
                            <h2>Söker …</h2>
                        </section>
                    ) : bestMatch ? (
                        <div className="faq-list" aria-live="polite">
                            <p className="faq-best-match-label">Närmaste svar på din sökning</p>
                            <article className="faq-accordion">
                                <h2 className="faq-item faq-item--static">{bestMatch.question}</h2>
                                <div className="faq-answer">
                                    <p>{bestMatch.answer}</p>
                                </div>
                            </article>
                        </div>
                    ) : (
                        <section className="faq-no-results" aria-live="polite">
                            <h2 tabIndex={0}>Inget svar hittades</h2>
                            <p>
                                {smartSearch.data?.message ??
                                    'Vi hittade ingen fråga som matchar din sökning. Testa att formulera om eller använd kontaktvägarna längre ner på sidan.'}
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
