import { z } from 'zod'

export const amountSchema = z.string().trim().superRefine((value, context) => {
    if (!value) {
        context.addIssue({
            code: 'custom',
            message: 'Ange ett belopp.',
        })
        return
    }

    const amount = Number(value.replace(',', '.'))

    if (Number.isNaN(amount)) {
        context.addIssue({
            code: 'custom',
            message: 'Ange ett giltigt belopp.',
        })
        return
    }

    if (amount <= 0) {
        context.addIssue({
            code: 'custom',
            message: 'Ange ett belopp som är större än 0 kr.',
        })
    }
})

export const transactionAmountSchema = z.object({
    amount: amountSchema,
})

export const transactSchema = z.object({
    accountId: z.string().trim().min(1, 'Välj ett konto.'),
    amount: amountSchema,
})
