import { createContext } from 'react'

export type Theme = 'light' | 'dark'

export type ThemeContextValue = {
    theme: Theme
    toggleTheme: () => void
}

export const ThemeContext = createContext<ThemeContextValue | undefined>(undefined)
