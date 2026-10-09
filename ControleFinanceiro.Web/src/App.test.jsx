import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App.jsx'
import FinancialResources from './FinancialResources.jsx'

const api = vi.hoisted(() => ({
  loadFinanceRecords: vi.fn(),
  loadDebtData: vi.fn(),
  loadFinancialResources: vi.fn(),
  loadTripBudgets: vi.fn(),
  signInOwner: vi.fn(),
  signOutOwner: vi.fn(),
}))

vi.mock('./financeApi.js', async (importOriginal) => ({
  ...(await importOriginal()),
  ...api,
}))

describe('finance application flows', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    api.loadFinanceRecords.mockResolvedValue({
      entradas: [],
      contasCasa: [],
      cartaoLancamentos: [],
      despesasVariaveis: [],
    })
    api.loadDebtData.mockResolvedValue({
      debts: [],
      summary: { cashOutflowAmount: 123.45, scheduledAmount: 0, paidAmount: 0, remainingAmount: 0 },
      recurringExpenses: [],
      recurringProjection: {
        occurrences: [{ id: 4, name: 'Annual renewal', amount: 40, renewalDate: '2026-10-15' }],
      },
    })
    api.loadFinancialResources.mockResolvedValue({ accounts: [], cards: [] })
    api.loadTripBudgets.mockResolvedValue([])
    api.signInOwner.mockResolvedValue(undefined)
    api.signOutOwner.mockResolvedValue(undefined)
  })

  it('signs in and loads the protected dashboard summary and recurring renewals', async () => {
    api.loadFinanceRecords
      .mockRejectedValueOnce(Object.assign(new Error('Unauthorized'), { status: 401 }))
      .mockResolvedValueOnce({
        entradas: [],
        contasCasa: [],
        cartaoLancamentos: [],
        despesasVariaveis: [],
      })
    const user = userEvent.setup()
    render(<App />)

    await user.type(await screen.findByLabelText('Usuário'), 'owner')
    await user.type(screen.getByLabelText('Senha'), 'not-a-real-password')
    await user.click(screen.getByRole('button', { name: 'Entrar' }))

    expect(await screen.findByRole('heading', { name: 'Visão geral' })).toBeInTheDocument()
    expect(api.signInOwner).toHaveBeenCalledWith('owner', 'not-a-real-password')
    expect(await screen.findByText('Annual renewal')).toBeInTheDocument()
    expect(screen.getAllByText(/123,45/)).toHaveLength(2)
  })

  it('shows only masked account identifiers in ordinary account views', () => {
    render(
      <FinancialResources
        accounts={[{
          id: 1,
          accountName: 'Checking',
          bankName: 'Example bank',
          accountType: 'Checking',
          branchIdentifier: '***23',
          accountIdentifier: '****7766',
          balance: 0,
          availableOverdraft: 0,
        }]}
        cards={[]}
        loading={false}
        error=""
        onSaved={vi.fn()}
      />,
    )

    expect(screen.getByText('Ag. ***23 · Conta ****7766')).toBeInTheDocument()
    expect(screen.queryByText(/00123|99887766/)).not.toBeInTheDocument()
  })

  it('navigates to trip budgeting while keeping the dashboard protected', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Viagens' }))

    expect(screen.getByRole('heading', { name: 'Novo orçamento' })).toBeInTheDocument()
    expect(await screen.findByText('Nenhuma viagem cadastrada.')).toBeInTheDocument()
  })

  it('signs out and returns to the protected sign-in screen', async () => {
    const user = userEvent.setup()
    render(<App />)

    await user.click(await screen.findByRole('button', { name: 'Sair' }))

    expect(api.signOutOwner).toHaveBeenCalledOnce()
    expect(await screen.findByRole('heading', { name: 'Acesse sua conta' })).toBeInTheDocument()
  })
})
