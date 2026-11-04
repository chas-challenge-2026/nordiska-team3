import { z } from 'zod'

export const createAccountSchema = z.object({
    name: z
        .string()
        .trim()
        .min(1, 'Ange ett kontonamn.')
        .max(40, 'Kontonamnet får vara max 40 tecken.'),
})
