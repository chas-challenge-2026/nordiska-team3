import { z } from 'zod'

export const loginSchema = z.object({
    personalNumber: z
        .string()
        .trim()
        .min(1, 'Fyll i personnummer.')
        .regex(/^\d{8}-\d{4}$/, 'Personnummer ska anges i formatet ÅÅÅÅMMDD-XXXX.'),
    pin: z
        .string()
        .trim()
        .min(1, 'Fyll i PIN-kod.')
        .regex(/^\d{4,6}$/, 'PIN-koden ska vara 4-6 siffror.'),
})
