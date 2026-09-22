import { getStoredUser } from '../services/authService'
import './UserProfile.css'

export function UserProfile() {
  const currentUser = getStoredUser() ?? {
    name: 'Emma Lindström',
    email: 'emma@exempel.se',
  }

  return (
    <div className="user-profile">
      <div className="user-profile-avatar">{currentUser.name.charAt(0)}</div>
      <div className="user-profile-text">
        <p className="user-profile-name">{currentUser.name}</p>
        <p className="user-profile-email">{currentUser.email}</p>
      </div>
    </div>
  )
}
