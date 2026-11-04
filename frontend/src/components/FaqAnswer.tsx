import type { ReactNode } from 'react'
import { Link, useLocation } from 'react-router-dom'
import type { LinkedModal } from '../hooks/useModalFromLink'

type AnswerLink = {
    text: string
    // Sidan länken går till. Saknas den stannar kunden på sidan, t.ex. för profilmodalen som finns överallt.
    to?: string
    // Modal som öppnas när kunden kommer fram
    modal?: LinkedModal
}

// Fraser i FAQ-svaren som pekar på en sida i appen. Svaren kommer som vanlig text från backend,
// så länkarna läggs på här och texten (och därmed sökningen) påverkas inte.
// Ordningen avgör vilken fras som blir länk när flera har samma mål. En länk med modal räknas
// som ett eget mål, så "Dashboard" går till sidan och "Skapa nytt sparkonto" öppnar modalen.
const answerLinks: AnswerLink[] = [
    { text: 'Skapa nytt sparkonto', to: '/dashboard', modal: 'createAccount' },
    { text: 'Dashboard', to: '/dashboard' },
    { text: 'Insättning/Uttag', to: '/transaktioner' },
    { text: 'Skatterapport', to: '/skatt' },
    { text: 'Historik', to: '/historik' },
    { text: 'Profil och inställningar', modal: 'profileSettings' },
    { text: '08-123 456 78', to: 'tel:+46812345678' },
    { text: 'hej@nordiska.se', to: 'mailto:hej@nordiska.se' },
]

type Match = AnswerLink & { to: string; index: number }

function findMatches(answer: string, currentPath: string) {
    const matches: Match[] = []
    const linkedTargets = new Set<string>()

    for (const link of answerLinks) {
        const to = link.to ?? currentPath
        const target = `${to}#${link.modal ?? ''}`

        // Varje mål länkas bara en gång per svar
        if (linkedTargets.has(target)) continue

        const index = answer.indexOf(link.text)
        if (index === -1) continue

        const overlaps = matches.some(
            (m) => index < m.index + m.text.length && m.index < index + link.text.length
        )
        if (overlaps) continue

        matches.push({ ...link, to, index })
        linkedTargets.add(target)
    }

    return matches.sort((a, b) => a.index - b.index)
}

type FaqAnswerProps = {
    answer: string
}

export function FaqAnswer({ answer }: FaqAnswerProps) {
    const { pathname } = useLocation()
    const parts: ReactNode[] = []
    let position = 0

    for (const match of findMatches(answer, pathname)) {
        parts.push(answer.slice(position, match.index))

        const isExternal = match.to.startsWith('tel:') || match.to.startsWith('mailto:')
        parts.push(
            isExternal ? (
                <a key={match.index} className="faq-answer-link" href={match.to}>
                    {match.text}
                </a>
            ) : (
                <Link
                    key={match.index}
                    className="faq-answer-link"
                    to={match.to}
                    state={match.modal ? { openModal: match.modal } : undefined}
                >
                    {match.text}
                </Link>
            )
        )

        position = match.index + match.text.length
    }

    parts.push(answer.slice(position))

    return <p>{parts}</p>
}
