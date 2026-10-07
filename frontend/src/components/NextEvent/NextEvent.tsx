import { Clock, Percent } from 'lucide-react'
import { formatEventDate, type NextEvent as NextEventData } from '../../utils/nextEvent'
import './NextEvent.css'

type NextEventProps = {
    event: NextEventData | null
}

export function NextEvent({ event }: NextEventProps) {
    // Rutan döljs helt när det inte finns någon kommande händelse
    if (!event) {
        return null
    }

    return (
        <section className="next-event" aria-labelledby="next-event-title">
            <h2 className="next-event__heading" id="next-event-title">
                Nästa händelse
            </h2>

            <article className="next-event__card">
                <div className={`next-event__icon next-event__icon--${event.kind}`} aria-hidden="true">
                    {event.kind === 'interest' ? <Percent size={16} strokeWidth={2.5} /> : <Clock size={16} strokeWidth={2.5} />}
                </div>

                <div className="next-event__content">
                    <h3>{event.title}</h3>
                    <p>{event.description}</p>
                </div>

                <div className="next-event__date">
                    <span>{event.dateLabel}</span>
                    {event.date ? (
                        <time dateTime={event.date.toLocaleDateString('sv-SE')}>{formatEventDate(event.date)}</time>
                    ) : (
                        <strong>Behandlas</strong>
                    )}
                </div>
            </article>
        </section>
    )
}
