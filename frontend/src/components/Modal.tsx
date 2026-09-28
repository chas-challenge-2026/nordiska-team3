import { useEffect, useId, useRef, type ReactNode } from 'react'
import { X } from 'lucide-react'
import './Modal.css'

interface ModalProps {
    isOpen: boolean
    onClose: () => void
    title?: string
    children: ReactNode
}

export function Modal({ isOpen, onClose, title, children }: ModalProps) {
    const titleId = useId()
    const modalRef = useRef<HTMLDivElement>(null)

    useEffect(() => {
        if (!isOpen) return

        const previouslyFocusedElement = document.activeElement instanceof HTMLElement ? document.activeElement : null
        const modalElement = modalRef.current

        if (!modalElement) return
        const dialogElement = modalElement

        const getFocusableElements = () =>
            Array.from(
                dialogElement.querySelectorAll<HTMLElement>(
                    'a[href], button:not([disabled]), textarea:not([disabled]), input:not([disabled]), select:not([disabled]), [tabindex]:not([tabindex="-1"])'
                )
            ).filter((element) => !element.hasAttribute('disabled') && element.offsetParent !== null)

        const focusableElements = getFocusableElements()
        const firstFocusableElement = focusableElements[0] ?? modalElement

        firstFocusableElement.focus()

        function handleKeyDown(event: KeyboardEvent) {
            if (event.key === 'Escape') {
                onClose()
                return
            }

            if (event.key !== 'Tab') return

            const currentFocusableElements = getFocusableElements()

            if (currentFocusableElements.length === 0) {
                event.preventDefault()
                dialogElement.focus()
                return
            }

            const firstElement = currentFocusableElements[0]
            const lastElement = currentFocusableElements[currentFocusableElements.length - 1]

            if (event.shiftKey && document.activeElement === firstElement) {
                event.preventDefault()
                lastElement.focus()
            }

            if (!event.shiftKey && document.activeElement === lastElement) {
                event.preventDefault()
                firstElement.focus()
            }
        }

        document.addEventListener('keydown', handleKeyDown)

        return () => {
            document.removeEventListener('keydown', handleKeyDown)
            previouslyFocusedElement?.focus()
        }
    }, [isOpen, onClose])

    if (!isOpen) return null

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div
                className="modal-card"
                ref={modalRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby={title ? titleId : undefined}
                tabIndex={-1}
                onClick={(e) => e.stopPropagation()}
            >
                <div className="modal-header">
                    {title && (
                        <h2 id={titleId} tabIndex={0}>
                            {title}
                        </h2>
                    )}
                    <button className="modal-close" onClick={onClose} aria-label="Stäng">
                        <X size={18} />
                    </button>
                </div>
                <div className="modal-body">{children}</div>
            </div>
        </div>
    )
}
