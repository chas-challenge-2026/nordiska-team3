import { type ReactNode, useEffect, useMemo, useState } from 'react'
import { ThemeContext, type Theme } from './themeTypes'

export function ThemeProvider({ children }: { children: ReactNode }) {
    const [theme, setTheme] = useState<Theme>(() => {
        const savedTheme = localStorage.getItem('theme')

        if (savedTheme === 'light' || savedTheme === 'dark') {
            return savedTheme
        }
        return 'light'
    })

    useEffect(() => {
        document.documentElement.dataset.theme = theme
        localStorage.setItem('theme', theme)
    }, [theme])

    const value = useMemo(
        () => ({
            theme,
            toggleTheme: () => {
                setTheme((currentTheme) =>
                currentTheme === 'light' ? 'dark' : 'light',
            )
            },
        }),
        [theme],
    )

    return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}
