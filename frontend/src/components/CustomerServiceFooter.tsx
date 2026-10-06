import { Clock, Mail, Phone } from 'lucide-react'
import { customerServiceContact } from '../content/customerServiceContact'
import './CustomerServiceFooter.css'

export function CustomerServiceFooter() {
    return (
        <footer className="customer-service-footer" aria-labelledby="customer-service-footer-title">
            <div className="customer-service-footer__inner">
                <div>
                    <h2 id="customer-service-footer-title">Behöver du hjälp?</h2>
                    <p>Kontakta kundservice om du har frågor eller behöver hjälp med ditt ärende.</p>
                </div>

                <div className="customer-service-footer__details" aria-label="Kontaktuppgifter till kundservice">
                    <a
                        href={customerServiceContact.phoneHref}
                        aria-label={`Ring kundservice på ${customerServiceContact.phone}`}
                        aria-describedby="customer-service-opening-hours"
                    >
                        <Phone size={15} aria-hidden="true" />
                        {customerServiceContact.phone}
                    </a>
                    <a
                        href={customerServiceContact.emailHref}
                        aria-label={`Mejla kundservice på ${customerServiceContact.email}`}
                        aria-describedby="customer-service-opening-hours"
                    >
                        <Mail size={15} aria-hidden="true" />
                        {customerServiceContact.email}
                    </a>
                    <span id="customer-service-opening-hours">
                        <Clock size={15} aria-hidden="true" />
                        <span className="customer-service-footer__sr-only">Öppettider: </span>
                        {customerServiceContact.openingHours}
                    </span>
                </div>
            </div>
        </footer>
    )
}
