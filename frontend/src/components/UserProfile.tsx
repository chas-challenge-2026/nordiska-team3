import { useState } from 'react'
import { getStoredUser } from '../services/authService'
import { useModalFromLink } from '../hooks/useModalFromLink'
import { ProfileSettingsModal } from './ProfileSettingsModal'
import './UserProfile.css'

export function UserProfile() {
  const [isModalOpen, setIsModalOpen] = useState(false)
  // FAQ-svaret "Profil och inställningar" öppnar modalen via en länk
  const profileSettingsLink = useModalFromLink('profileSettings')

  const currentUser = getStoredUser() ?? {
    name: 'Emma Lindström',
    email: 'emma@exempel.se',
    personalNumber: '19900101-1234',
}

  return (
    <>
      <button
        type="button"
        className="user-profile user-profile--clickable"
        onClick={() => setIsModalOpen(true)}
      >
        <div className="user-profile-avatar">{currentUser.name.charAt(0)}</div>
        <div className="user-profile-text">
          <p className="user-profile-name">{currentUser.name}</p>
          <p className="user-profile-email">{currentUser.email}</p>
        </div>
      </button>

      <ProfileSettingsModal
        isOpen={isModalOpen || profileSettingsLink.isRequested}
        onClose={() => {
          setIsModalOpen(false)
          profileSettingsLink.clearRequest()
        }}
        user={currentUser}
      />
    </>
  )
}