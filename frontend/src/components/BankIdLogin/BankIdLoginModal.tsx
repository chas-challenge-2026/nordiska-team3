import { useEffect, useRef, useState } from 'react'
import { QRCodeSVG } from 'qrcode.react'
import { Modal } from '../Modal'
import { Button } from '../Button'
import { MockApprovePanel } from './MockApprovePanel'
import { cancelBankId, getBankIdStatus, startBankId } from '../../services/authService'
import './BankIdLogin.css'

interface BankIdLoginModalProps {
    isOpen: boolean
    onClose: () => void
    onSuccess: () => void
}

type BankIdState =
    | { phase: 'starting' }
    | { phase: 'pending'; orderId: string; mock: boolean; qrData: string | null; hintCode: string | null }
    | { phase: 'failed'; message: string }

const POLL_INTERVAL_MS = 1000

// Texter enligt BankIDs rekommenderade användarmeddelanden.
function pendingMessage(hintCode: string | null) {
    if (hintCode === 'userSign') return 'Skriv in din säkerhetskod i BankID-appen och välj Identifiera.'
    if (hintCode === 'started') return 'Söker efter BankID, det kan ta en liten stund...'
    return 'Starta BankID-appen och skanna QR-koden.'
}

function failedMessage(hintCode: string | null) {
    switch (hintCode) {
        case 'userCancel':
            return 'Inloggningen avbröts.'
        case 'expiredTransaction':
            return 'BankID-appen svarade inte i tid. Försök igen.'
        case 'startFailed':
            return 'BankID-appen startades inte. Försök igen.'
        case 'userNotFound':
            return 'Det finns inget konto kopplat till det här personnumret.'
        case 'unknownOrder':
            return 'Inloggningen har gått ut. Försök igen.'
        default:
            return 'Inloggningen med BankID misslyckades. Försök igen.'
    }
}

export function BankIdLoginModal({ isOpen, onClose, onSuccess }: BankIdLoginModalProps) {
    const [state, setState] = useState<BankIdState>({ phase: 'starting' })
    // Ökas vid "Försök igen" så att effekten startar en ny order.
    const [attempt, setAttempt] = useState(0)
    const onSuccessRef = useRef(onSuccess)

    useEffect(() => {
        onSuccessRef.current = onSuccess
    }, [onSuccess])

    useEffect(() => {
        if (!isOpen) return

        let active = true
        let finished = false
        let orderId: string | null = null
        let timer: ReturnType<typeof setTimeout> | undefined

        async function poll(id: string, mock: boolean) {
            try {
                const status = await getBankIdStatus(id)
                if (!active) return

                if (status.status === 'pending') {
                    setState({ phase: 'pending', orderId: id, mock, qrData: status.qrData, hintCode: status.hintCode })
                    timer = setTimeout(() => poll(id, mock), POLL_INTERVAL_MS)
                    return
                }

                finished = true

                if (status.status === 'complete') {
                    // Inloggningen är redan sparad av authService.
                    onSuccessRef.current()
                    return
                }

                setState({ phase: 'failed', message: failedMessage(status.hintCode) })
            } catch (error) {
                if (!active) return
                setState({ phase: 'failed', message: error instanceof Error ? error.message : failedMessage(null) })
            }
        }

        async function start() {
            try {
                const order = await startBankId()

                if (!active) {
                    // Modalen stängdes medan start pågick, så ordern ska inte ligga kvar.
                    void cancelBankId(order.orderId)
                    return
                }

                orderId = order.orderId
                await poll(order.orderId, order.mock)
            } catch (error) {
                if (!active) return
                finished = true
                setState({ phase: 'failed', message: error instanceof Error ? error.message : failedMessage(null) })
            }
        }

        void start()

        return () => {
            active = false
            clearTimeout(timer)

            if (orderId && !finished) {
                void cancelBankId(orderId)
            }
        }
    }, [isOpen, attempt])

    // State nollställs här i stället för i effekten, så att nästa öppning börjar på "Startar BankID...".
    function handleRetry() {
        setState({ phase: 'starting' })
        setAttempt((value) => value + 1)
    }

    function handleClose() {
        setState({ phase: 'starting' })
        onClose()
    }

    return (
        <Modal isOpen={isOpen} onClose={handleClose} title="Logga in med BankID">
            <div className="bankid-login">
                {state.phase === 'starting' && <p className="bankid-message">Startar BankID...</p>}

                {state.phase === 'pending' && (
                    <>
                        <div className="bankid-qr">
                            {state.qrData ? (
                                <QRCodeSVG value={state.qrData} size={200} marginSize={2} title="QR-kod för BankID" />
                            ) : (
                                <div className="bankid-qr-placeholder" />
                            )}
                        </div>

                        <p className="bankid-message" role="status">
                            {pendingMessage(state.hintCode)}
                        </p>

                        {state.mock && <MockApprovePanel orderId={state.orderId} />}
                    </>
                )}

                {state.phase === 'failed' && (
                    <>
                        <p className="bankid-message bankid-message--error" role="alert">
                            {state.message}
                        </p>
                        <Button type="button" variant="primary" onClick={handleRetry}>
                            Försök igen
                        </Button>
                    </>
                )}

                <Button type="button" variant="secondary" onClick={handleClose}>
                    Avbryt
                </Button>
            </div>
        </Modal>
    )
}
