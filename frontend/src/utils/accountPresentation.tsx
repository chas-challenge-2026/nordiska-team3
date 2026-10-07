import {
    Banknote,
    Briefcase,
    Car,
    Gem,
    Gift,
    GraduationCap,
    Heart,
    Home,
    Landmark,
    Laptop,
    PiggyBank,
    Plane,
    Shield,
    Target,
    Umbrella,
    Wallet,
} from 'lucide-react'

export type AccountIconId =
    | 'piggyBank'
    | 'graduationCap'
    | 'plane'
    | 'shield'
    | 'wallet'
    | 'home'
    | 'car'
    | 'gift'
    | 'heart'
    | 'briefcase'
    | 'laptop'
    | 'target'
    | 'umbrella'
    | 'landmark'
    | 'banknote'
    | 'gem'

export type AccountVariant = 'default' | 'accent' | 'success' | 'danger' | 'purple' | 'pink'

export type AccountPresentation = {
    iconId: AccountIconId
    variant: AccountVariant
}

type AccountPresentationMap = Record<string, AccountPresentation>

export const accountIconOptions: { id: AccountIconId; label: string; icon: JSX.Element }[] = [
    { id: 'piggyBank', label: 'Sparande', icon: <PiggyBank size={16} /> },
    { id: 'graduationCap', label: 'Studier', icon: <GraduationCap size={16} /> },
    { id: 'plane', label: 'Resa', icon: <Plane size={16} /> },
    { id: 'shield', label: 'Buffert', icon: <Shield size={16} /> },
    { id: 'wallet', label: 'Plånbok', icon: <Wallet size={16} /> },
    { id: 'home', label: 'Bostad', icon: <Home size={16} /> },
    { id: 'car', label: 'Bil', icon: <Car size={16} /> },
    { id: 'gift', label: 'Presenter', icon: <Gift size={16} /> },
    { id: 'heart', label: 'Familj', icon: <Heart size={16} /> },
    { id: 'briefcase', label: 'Jobb', icon: <Briefcase size={16} /> },
    { id: 'laptop', label: 'Teknik', icon: <Laptop size={16} /> },
    { id: 'target', label: 'Mål', icon: <Target size={16} /> },
    { id: 'umbrella', label: 'Trygghet', icon: <Umbrella size={16} /> },
    { id: 'landmark', label: 'Bank', icon: <Landmark size={16} /> },
    { id: 'banknote', label: 'Pengar', icon: <Banknote size={16} /> },
    { id: 'gem', label: 'Lyx', icon: <Gem size={16} /> },
]

export const accountVariantOptions: { value: AccountVariant; label: string }[] = [
    { value: 'default', label: 'Blå' },
    { value: 'accent', label: 'Orange' },
    { value: 'success', label: 'Grön' },
    { value: 'danger', label: 'Röd' },
    { value: 'purple', label: 'Lila' },
    { value: 'pink', label: 'Rosa' },
]

const storageKey = 'nordiska.accountPresentation.v1'

function readAccountPresentations(): AccountPresentationMap {
    const storedPresentations = localStorage.getItem(storageKey)

    if (!storedPresentations) {
        return {}
    }

    try {
        return JSON.parse(storedPresentations) as AccountPresentationMap
    } catch {
        return {}
    }
}

function saveAccountPresentations(presentations: AccountPresentationMap) {
    localStorage.setItem(storageKey, JSON.stringify(presentations))
}

export function getAccountIcon(iconId: AccountIconId) {
    return accountIconOptions.find((option) => option.id === iconId)?.icon ?? <PiggyBank size={16} />
}

export function getDefaultAccountPresentation(index: number): AccountPresentation {
    return {
        iconId: accountIconOptions[index % accountIconOptions.length].id,
        variant: accountVariantOptions[index % accountVariantOptions.length].value,
    }
}

export function getAccountPresentation(accountId: string, index: number) {
    const presentations = readAccountPresentations()
    const presentation = presentations[accountId] ?? getDefaultAccountPresentation(index)

    if (!presentations[accountId]) {
        saveAccountPresentations({
            ...presentations,
            [accountId]: presentation,
        })
    }

    return presentation
}

export function setAccountPresentation(accountId: string, presentation: AccountPresentation) {
    saveAccountPresentations({
        ...readAccountPresentations(),
        [accountId]: presentation,
    })
}
