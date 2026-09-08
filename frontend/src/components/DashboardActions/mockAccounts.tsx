import { GraduationCap, Plane, Shield } from 'lucide-react'
import type { Account } from './BalanceOverview'

export const mockAccounts: Account[] = [
    {
        id: 'education',
        icon: <GraduationCap size={16} />,
        label: 'HÖGSKOLESPARANDE',
        value: '45 231 kr',
        variant: 'default',
    },
    {
        id: 'vacation',
        icon: <Plane size={16} />,
        label: 'SEMESTERFONDEN',
        value: '12 800 kr',
        variant: 'accent',
    },
    {
        id: 'buffer',
        icon: <Shield size={16} />,
        label: 'BUFFERTSPARANDE',
        value: '78 541 kr',
        variant: 'success',
    },
]