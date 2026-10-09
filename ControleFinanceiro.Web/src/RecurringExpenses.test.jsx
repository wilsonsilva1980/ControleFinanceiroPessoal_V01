import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import RecurringExpenses from './RecurringExpenses.jsx'

const createRecurringExpense = vi.hoisted(() => vi.fn())

vi.mock('./financeApi.js', async (importOriginal) => ({
  ...(await importOriginal()),
  createRecurringExpense,
}))

describe('recurring charge forms', () => {
  it('submits annual cadence anchored by the selected initial date', async () => {
    createRecurringExpense.mockResolvedValueOnce({ id: 1 })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(<RecurringExpenses expenses={[]} loading={false} error="" onSaved={onSaved} />)

    await user.type(screen.getByLabelText('Nome'), 'Annual service')
    await user.type(screen.getByLabelText('Valor'), '120')
    await user.type(screen.getByLabelText('Forma de pagamento'), 'Cartão')
    await user.selectOptions(screen.getByLabelText('Periodicidade'), 'Yearly')
    fireEvent.change(screen.getByLabelText('Primeira renovação'), { target: { value: '2026-10-31' } })
    await user.click(screen.getByRole('button', { name: 'Salvar cobrança' }))

    await waitFor(() => expect(createRecurringExpense).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Annual service',
      cadence: 'Yearly',
      initialRenewalDate: '2026-10-31',
      amount: 120,
    })))
    expect(onSaved).toHaveBeenCalledOnce()
  })
})
