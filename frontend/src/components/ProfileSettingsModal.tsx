import { useState } from 'react'
import { Modal } from './Modal'
import { Input } from './Input'
import { Button } from './Button'
import { useLogout } from '../hooks/useLogout'
import { LogOut } from 'lucide-react'
import './ProfileSettingsModal.css'

interface ProfileUser {
    name: string
    email: string
    personalNumber?: string
}

interface ProfileSettingsModalProps {
    isOpen: boolean
    onClose: () => void
    user: ProfileUser
}

function maskPersonalNumber(pn?: string) {
    if (!pn) return 'Ej tillgängligt'
    // Visar bara de fyra sista siffrorna, t.ex. 19900101-XXXX
    const parts = pn.split('-')
    if (parts.length === 2) return `${parts[0]}-XXXX`
    return `${pn.slice(0, -4)}XXXX`
}

export function ProfileSettingsModal({ isOpen, onClose, user }: ProfileSettingsModalProps) {
    const handleLogout = useLogout()

    const [name, setName] = useState(user.name)
    const [email, setEmail] = useState(user.email)
    const [saved, setSaved] = useState(false)

    function handleSave() {
        // Mock: sparar bara lokalt i state tills vidare, ingen backend-koppling än
        setSaved(true)
        setTimeout(() => setSaved(false), 2000)
    }

    return (
        <Modal isOpen={isOpen} onClose={onClose} title="Profil och inställningar">
            <div className="profile-settings">
                <div className="profile-settings-avatar">
                    {name.charAt(0)}
                </div>

                <div className="profile-settings-field">
                    <label className="input-label">Personnummer</label>
                    <p className="profile-settings-readonly">{maskPersonalNumber(user.personalNumber)}</p>
                </div>

                <Input
                    label="Namn"
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                />

                <Input
                    label="E-post"
                    type="text"
                    value={email}
                    onChange={(e) => setEmail(e.target.value)}
                />

                {saved && <p className="profile-settings-success">Sparat!</p>}

                <Button type="button" variant="primary" onClick={handleSave}>
                    Spara ändringar
                </Button>

                <button type="button" className="profile-settings-logout" onClick={handleLogout}>
                    <LogOut size={16} />
                    Logga ut
                </button>
            </div>
        </Modal>
    )
}