import { ArrowDownLeft, ArrowUpRight } from 'lucide-react'
import './RecentEvents.css'
import { mockRecentEvents, type RecentEvent } from './mockRecentEvents'

type RecentEventsProps = {
  events?: RecentEvent[]
}

export function RecentEvents({ events = mockRecentEvents }: RecentEventsProps) {
  return (
    <section className="recent-events">
      <h2 className="recent-events__title">Senaste händelser</h2>

      <div className="recent-events__list">
        {events.map((event) => (
          <article className="recent-events__item" key={event.id}>
            <div
            className={`recent-events__icon ${event.amount < 0 ? 'recent-events__icon--negative' : ''}`}
            aria-hidden="true"
            >
              {event.amount >= 0 ? (
                <ArrowDownLeft size={14} strokeWidth={2.5} />
              ) : (
                <ArrowUpRight size={14} strokeWidth={2.5} />
              )}
            </div>

            <div className="recent-events__content">
              <h3 tabIndex={0}>{event.title}</h3>
              <p tabIndex={0}>
                {event.accountName} - {event.date}
              </p>
            </div>

            <div className="recent-events__meta">
              <strong
                className={event.amount >= 0 ? 'is-positive' : 'is-negative'}
                tabIndex={0}
              >
                {formatAmount(event.amount)}
              </strong>
              <span
                className={`recent-events__badge recent-events__badge--${event.type}`}
                tabIndex={0}
              >
                {getEventLabel(event.type)}
              </span>
            </div>
          </article>
        ))}
      </div>
    </section>
  )
}

function formatAmount(amount: number) {
  const sign = amount > 0 ? '+' : ''
  return `${sign}${amount.toLocaleString('sv-SE', {
    minimumFractionDigits: 2,
    maximumFractionDigits: 2,
  })} kr`
}

function getEventLabel(type: RecentEvent['type']) {
  if (type === 'deposit') {
    return 'Insättning'
  }

  if (type === 'withdrawal') {
    return 'Uttag'
  }

  return 'Ränta'
}
