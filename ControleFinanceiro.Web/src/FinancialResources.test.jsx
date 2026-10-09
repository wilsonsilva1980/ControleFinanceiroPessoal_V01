import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { describe, expect, it, vi } from 'vitest'
import FinancialResources from './FinancialResources.jsx'

const api = vi.hoisted(() => ({
  createBankAccount: vi.fn(),
  createCreditCard: vi.fn(),
}))

vi.mock('./financeApi.js', async (importOriginal) => ({
  ...(await importOriginal()),
  ...api,
}))

describe('financial resource forms and summaries', () => {
  it('creates a bank account with numeric balance and overdraft values', async () => {
    api.createBankAccount.mockResolvedValueOnce({ id: 1 })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(<FinancialResources accounts={[]} cards={[]} loading={false} error="" onSaved={onSaved} />)

    await user.type(screen.getByLabelText('Instituição'), 'Example Bank')
    await user.type(screen.getByLabelText('Nome da conta'), 'Checking')
    await user.type(screen.getByLabelText('Agência'), '00123')
    await user.type(screen.getByLabelText('Conta'), '99887766')
    await user.type(screen.getByLabelText('Saldo'), '1250.5')
    await user.clear(screen.getByLabelText('Cheque especial utilizado'))
    await user.type(screen.getByLabelText('Cheque especial utilizado'), '50')
    await user.clear(screen.getByLabelText('Limite de cheque especial'))
    await user.type(screen.getByLabelText('Limite de cheque especial'), '500')
    await user.click(screen.getByRole('button', { name: 'Salvar conta' }))

    await waitFor(() => expect(api.createBankAccount).toHaveBeenCalledWith(expect.objectContaining({
      bankName: 'Example Bank',
      branchIdentifier: '00123',
      accountIdentifier: '99887766',
      balance: 1250.5,
      overdraftUsed: 50,
      overdraftLimit: 500,
    })))
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('creates a credit card with its limits and payment capabilities', async () => {
    api.createCreditCard.mockResolvedValueOnce({ id: 2 })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(<FinancialResources accounts={[]} cards={[]} loading={false} error="" onSaved={onSaved} />)

    await user.type(screen.getByLabelText('Emissor'), 'Example issuer')
    await user.type(screen.getByLabelText('Nome do cartão'), 'Travel card')
    await user.type(screen.getByLabelText('Limite de compras'), '2000')
    await user.clear(screen.getByLabelText('Saque nacional'))
    await user.type(screen.getByLabelText('Saque nacional'), '100')
    await user.clear(screen.getByLabelText('Saque internacional'))
    await user.type(screen.getByLabelText('Saque internacional'), '250')
    await user.clear(screen.getByLabelText('Dia de vencimento'))
    await user.type(screen.getByLabelText('Dia de vencimento'), '12')
    await user.click(screen.getByLabelText('Pix no crédito'))
    await user.click(screen.getByLabelText('Pagamento de contas'))
    await user.click(screen.getByRole('button', { name: 'Salvar cartão' }))

    await waitFor(() => expect(api.createCreditCard).toHaveBeenCalledWith(expect.objectContaining({
      issuer: 'Example issuer',
      displayName: 'Travel card',
      purchaseLimit: 2000,
      localCashWithdrawalLimit: 100,
      internationalCashWithdrawalLimit: 250,
      dueDay: 12,
      creditPixEnabled: true,
      billPaymentEnabled: true,
    })))
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('renders combined account balances, available overdraft, and card limits', () => {
    render(
      <FinancialResources
        accounts={[
          { id: 1, accountName: 'Checking', bankName: 'Bank', accountType: 'Conta corrente', branchIdentifier: '***23', accountIdentifier: '****66', balance: 1250, availableOverdraft: 450 },
          { id: 2, accountName: 'Savings', bankName: 'Bank', accountType: 'Poupança', branchIdentifier: '***23', accountIdentifier: '****77', balance: 750, availableOverdraft: 0 },
        ]}
        cards={[{ id: 3, displayName: 'Travel card', issuer: 'Issuer', dueDay: 12, creditPixEnabled: false, billPaymentEnabled: true, purchaseLimit: 2000 }]}
        loading={false}
        error=""
        onSaved={vi.fn()}
      />,
    )

    expect(screen.getByText('Saldo registrado').nextElementSibling).toHaveTextContent('R$ 2.000,00')
    expect(screen.getByText('Cheque especial disponível').nextElementSibling).toHaveTextContent('R$ 450,00')
    expect(screen.getByText('Limites de compras').nextElementSibling).toHaveTextContent('R$ 2.000,00')
  })
})
