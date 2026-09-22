import type { ReactNode } from 'react'
import { X } from 'lucide-react'
import './Modal.css'

interface ModalProps {
    isOpen: boolean
    onClose: () => void
    title: string
    children: ReactNode
}

export function Modal({ isOpen, onClose, title, children }: ModalProps) {
    if (!isOpen) return null

    return (
        <div className="modal-overlay" onClick={onClose}>
            <div className="modal-card" onClick={(e) => e.stopPropagation()}>
                <div className="modal-header">
                    <h2 tabIndex={0}>{title}</h2>
                    <button className="modal-close" onClick={onClose} aria-label="Stäng">
                        <X size={18} />
                    </button>
                </div>
                <div className="modal-body">{children}</div>
            </div>
        </div>
    )
}
