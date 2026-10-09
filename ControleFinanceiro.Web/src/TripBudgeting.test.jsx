import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import TripBudgeting from './TripBudgeting.jsx'

const api = vi.hoisted(() => ({
  createTripBudget: vi.fn(),
  createTripCostItem: vi.fn(),
  createTripPayment: vi.fn(),
}))

vi.mock('./financeApi.js', async (importOriginal) => ({
  ...(await importOriginal()),
  ...api,
}))

describe('trip budget forms', () => {
  it('creates a trip budget with its retained CLP quote', async () => {
    api.createTripBudget.mockResolvedValueOnce({ id: 4 })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(<TripBudgeting budgets={[]} loading={false} error="" onSaved={onSaved} />)

    await user.type(screen.getByLabelText('Nome da viagem'), 'Chile')
    await user.type(screen.getByLabelText('Destino'), 'Santiago')
    await user.type(screen.getByLabelText('Cotação CLP por BRL (opcional)'), '200')
    await user.click(screen.getByRole('button', { name: 'Criar viagem' }))

    await waitFor(() => expect(api.createTripBudget).toHaveBeenCalledWith(expect.objectContaining({
      name: 'Chile',
      destination: 'Santiago',
      clpPerBrlQuote: 200,
    })))
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('renders trip totals, payments, remaining balance, and CLP estimate', () => {
    render(
      <TripBudgeting
        budgets={[{
          id: 3,
          name: 'Chile',
          destination: 'Santiago',
          clpPerBrlQuote: 200,
          clpTotal: 22000,
          totalBRL: 110,
          paidAmount: 25,
          remainingAmount: 85,
          items: [],
        }]}
        loading={false}
        error=""
        onSaved={vi.fn()}
      />,
    )

    expect(screen.getByText('Total').nextElementSibling).toHaveTextContent('R$ 110,00')
    expect(screen.getByText('Pago').nextElementSibling).toHaveTextContent('R$ 25,00')
    expect(screen.getByText('Restante').nextElementSibling).toHaveTextContent('R$ 85,00')
    expect(screen.getByText('Total estimado em CLP').nextElementSibling).toHaveTextContent('22.000 CLP')
  })

  it('creates trip cost installments whose scheduled amounts equal value plus fees', async () => {
    api.createTripCostItem.mockResolvedValueOnce({ id: 9 })
    const user = userEvent.setup()
    render(
      <TripBudgeting
        budgets={[{
          id: 3,
          name: 'Chile',
          destination: 'Santiago',
          clpPerBrlQuote: 200,
          clpTotal: 0,
          totalBRL: 0,
          paidAmount: 0,
          remainingAmount: 0,
          items: [],
        }]}
        loading={false}
        error=""
        onSaved={vi.fn()}
      />,
    )

    await user.type(screen.getByLabelText('Descrição'), 'Hotel')
    await user.type(screen.getByLabelText('Valor (BRL)'), '100')
    await user.clear(screen.getByLabelText('Taxas (BRL)'))
    await user.type(screen.getByLabelText('Taxas (BRL)'), '10')
    await user.clear(screen.getByLabelText('Quantidade de parcelas'))
    await user.type(screen.getByLabelText('Quantidade de parcelas'), '3')
    fireEvent.change(screen.getByLabelText('Vencimento da 1ª parcela'), { target: { value: '2026-01-31' } })
    await user.click(screen.getByRole('button', { name: 'Adicionar item' }))

    await waitFor(() => expect(api.createTripCostItem).toHaveBeenCalledOnce())
    const [, payload] = api.createTripCostItem.mock.calls[0]
    expect(payload.installments).toHaveLength(3)
    expect(payload.installments.map((installment) => installment.amount)).toEqual([36.66, 36.66, 36.68])
    expect(payload.installments.map((installment) => installment.dueDate.slice(0, 10))).toEqual([
      '2026-01-31',
      '2026-02-28',
      '2026-03-31',
    ])
  })

  it('records payments against a scheduled trip installment', async () => {
    api.createTripPayment.mockResolvedValueOnce({ id: 11 })
    const user = userEvent.setup()
    render(
      <TripBudgeting
        budgets={[{
          id: 3,
          name: 'Chile',
          destination: 'Santiago',
          clpPerBrlQuote: 200,
          clpTotal: 22000,
          totalBRL: 110,
          paidAmount: 0,
          remainingAmount: 110,
          items: [{
            id: 9,
            description: 'Hotel',
            valueBRL: 100,
            feesBRL: 10,
            totalBRL: 110,
            paidAmount: 0,
            remainingAmount: 110,
            installmentCount: 1,
            installments: [{ id: 7, dueDate: '2026-10-31', amount: 110 }],
            payments: [],
          }],
        }]}
        loading={false}
        error=""
        onSaved={vi.fn()}
      />,
    )

    await user.type(screen.getByLabelText('Valor pago'), '50')
    await user.selectOptions(screen.getByLabelText('Parcela'), '7')
    await user.click(screen.getByRole('button', { name: 'Registrar pagamento' }))

    await waitFor(() => expect(api.createTripPayment).toHaveBeenCalledWith(9, expect.objectContaining({
      amount: 50,
      tripInstallmentId: 7,
      paymentDate: expect.stringMatching(/Z$/),
    })))
  })
})
