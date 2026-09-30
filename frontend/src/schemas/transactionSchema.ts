import { z } from 'zod'

export const transactionAmountSchema = z.object({
    amount: z
        .string()
        .trim()
        .min(1, 'Ange ett belopp.')
        .refine((value) => {
            const amount = Number(value.replace(',', '.'))

            return !Number.isNaN(amount) && amount > 0
        }, 'Ange ett giltigt belopp större än 0 kr.'),
})

export const transactSchema = z.object({
    accountId: z.string().trim().min(1, 'Välj ett konto.'),
    amount: z
        .string()
        .trim()
        .min(1, 'Ange ett belopp.')
        .refine((value) => {
            const numericAmount = Number(value.replace(',', '.'))

            return !Number.isNaN(numericAmount) && numericAmount > 0
        }, 'Ange ett giltigt belopp större än 0.'),
})
