import { type FormEvent, useState } from 'react'
import { Button } from '../Button'
import { Input } from '../Input'
import { loginSchema } from '../../schemas/loginSchema'
import { mockApproveBankId } from '../../services/authService'

interface MockApprovePanelProps {
    orderId: string
}

// Utvecklarpanel som bara visas när servern kör mock-BankID. Ersätter telefonen som skannar QR-koden.
// Nästa statusfråga efter godkännandet ger "complete" och inloggningen sker som vanligt.
export function MockApprovePanel({ orderId }: MockApprovePanelProps) {
    const [personalNumber, setPersonalNumber] = useState('')
    const [errorMessage, setErrorMessage] = useState('')
    const [isApproving, setIsApproving] = useState(false)

    async function handleApprove(event: FormEvent<HTMLFormElement>) {
        event.preventDefault()

        const validation = loginSchema.shape.personalNumber.safeParse(personalNumber)

        if (!validation.success) {
            setErrorMessage(validation.error.issues[0]?.message ?? 'Kontrollera personnumret.')
            return
        }

        setErrorMessage('')
        setIsApproving(true)

        try {
            await mockApproveBankId(orderId, validation.data)
        } catch (error) {
            setErrorMessage(error instanceof Error ? error.message : 'Mock-godkännandet misslyckades.')
        } finally {
            setIsApproving(false)
        }
    }

    return (
        <form className="bankid-mock-panel" onSubmit={handleApprove}>
            <p className="bankid-mock-title">Mock-läge (utveckling)</p>

            <Input
                label="Personnummer"
                type="text"
                placeholder="ÅÅÅÅMMDD-XXXX"
                value={personalNumber}
                error={errorMessage}
                onChange={(event) => setPersonalNumber(event.target.value)}
            />

            <Button type="submit" variant="primary" disabled={isApproving}>
                {isApproving ? 'Godkänner...' : 'Simulera godkännande'}
            </Button>
        </form>
    )
}
