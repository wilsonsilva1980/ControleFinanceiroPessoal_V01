import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import DebtManagement from './DebtManagement.jsx'

const { createDebt, previewDebtWorkbook, importDebtWorkbook } = vi.hoisted(() => ({
  createDebt: vi.fn(),
  previewDebtWorkbook: vi.fn(),
  importDebtWorkbook: vi.fn(),
}))

vi.mock('./financeApi.js', async (importOriginal) => ({
  ...(await importOriginal()),
  createDebt,
  previewDebtWorkbook,
  importDebtWorkbook,
}))

describe('debt management forms and summaries', () => {
  beforeEach(() => {
    createDebt.mockReset()
    previewDebtWorkbook.mockReset()
    importDebtWorkbook.mockReset()
  })

  it('creates a debt with its dated installment schedule', async () => {
    createDebt.mockResolvedValueOnce({ id: 8 })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{ scheduledAmount: 250, paidAmount: 0, remainingAmount: 250, cashOutflowAmount: 250 }}
        loading={false}
        error=""
        month="2026-10"
        onSaved={onSaved}
      />,
    )

    await user.type(screen.getByLabelText('Descrição'), 'Laptop')
    fireEvent.change(screen.getByLabelText('Vencimento final'), { target: { value: '2026-10-31' } })
    await user.type(screen.getByLabelText('Valor original'), '250')
    await user.type(screen.getByLabelText('Limite de crédito'), '500')
    fireEvent.change(screen.getByLabelText('Vencimento'), { target: { value: '2026-10-15' } })
    await user.type(screen.getByLabelText('Valor previsto'), '250')
    await user.click(screen.getByRole('button', { name: 'Salvar dívida' }))

    await waitFor(() => expect(createDebt).toHaveBeenCalledWith({
      description: 'Laptop',
      dueDate: '2026-10-31T00:00:00Z',
      originalAmount: 250,
      creditLimit: 500,
      installments: [{ dueDate: '2026-10-15T00:00:00Z', scheduledAmount: 250 }],
    }))
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('renders scheduled, paid, remaining, and cash outflow totals', () => {
    render(
      <DebtManagement
        debts={[]}
        summary={{ scheduledAmount: 125, paidAmount: 25, remainingAmount: 100, cashOutflowAmount: 125 }}
        loading={false}
        error=""
        month="2026-10"
        onSaved={vi.fn()}
      />,
    )

    expect(screen.getByText('Parcelas previstas').previousElementSibling).toHaveTextContent('R$ 125,00')
    expect(screen.getByText('Pago').nextElementSibling).toHaveTextContent('R$ 25,00')
    expect(screen.getByText('Restante').nextElementSibling).toHaveTextContent('R$ 100,00')
    expect(screen.getByText('Saídas no mês').nextElementSibling).toHaveTextContent('R$ 125,00')
  })

  it('previews workbook counts without importing before explicit confirmation', async () => {
    const file = new File(['synthetic workbook'], 'debts.xlsx')
    previewDebtWorkbook.mockResolvedValueOnce({
      debtCount: 2,
      installmentCount: 8,
      undatedEarlyPaymentsSkipped: 1,
    })
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{}}
        loading={false}
        error=""
        month="2026-10"
        onSaved={vi.fn()}
      />,
    )

    await user.upload(screen.getByLabelText('Arquivo Excel (.xlsx)'), file)
    await user.click(screen.getByRole('button', { name: 'Validar e pré-visualizar' }))

    await waitFor(() => expect(previewDebtWorkbook).toHaveBeenCalledWith(file))
    expect(importDebtWorkbook).not.toHaveBeenCalled()
    expect(screen.getByText('Dívidas encontradas no arquivo').nextElementSibling).toHaveTextContent('2')
    expect(screen.getByText('Parcelas encontradas no arquivo').nextElementSibling).toHaveTextContent('8')
    expect(screen.getByText('Antecipações sem data ignoradas').nextElementSibling).toHaveTextContent('1')
    expect(screen.getByRole('button', { name: 'Confirmar importação' })).toBeInTheDocument()
  })

  it('cancels a preview without importing the workbook', async () => {
    previewDebtWorkbook.mockResolvedValueOnce({
      debtCount: 1,
      installmentCount: 3,
      undatedEarlyPaymentsSkipped: 0,
    })
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{}}
        loading={false}
        error=""
        month="2026-10"
        onSaved={vi.fn()}
      />,
    )

    await user.upload(screen.getByLabelText('Arquivo Excel (.xlsx)'), new File(['fixture'], 'debts.xlsx'))
    await user.click(screen.getByRole('button', { name: 'Validar e pré-visualizar' }))
    await screen.findByText('Dívidas encontradas no arquivo')
    await user.click(screen.getByRole('button', { name: 'Cancelar' }))

    expect(screen.queryByText('Dívidas encontradas no arquivo')).not.toBeInTheDocument()
    expect(importDebtWorkbook).not.toHaveBeenCalled()
  })

  it('imports the selected workbook only after confirmation and shows aggregate results', async () => {
    const file = new File(['synthetic workbook'], 'debts.xlsx')
    previewDebtWorkbook.mockResolvedValueOnce({
      debtCount: 2,
      installmentCount: 8,
      undatedEarlyPaymentsSkipped: 1,
    })
    importDebtWorkbook.mockResolvedValueOnce({
      debtsInserted: 1,
      installmentsInserted: 4,
      alreadyImported: 5,
      undatedEarlyPaymentsSkipped: 1,
      batchId: 'batch-123',
    })
    const onSaved = vi.fn()
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{}}
        loading={false}
        error=""
        month="2026-10"
        onSaved={onSaved}
      />,
    )

    await user.upload(screen.getByLabelText('Arquivo Excel (.xlsx)'), file)
    await user.click(screen.getByRole('button', { name: 'Validar e pré-visualizar' }))
    await screen.findByText('Dívidas encontradas no arquivo')
    await user.click(screen.getByRole('button', { name: 'Confirmar importação' }))

    await waitFor(() => expect(importDebtWorkbook).toHaveBeenCalledWith(file))
    expect(screen.getByText('Importação concluída')).toBeInTheDocument()
    expect(screen.getByText('1 dívida e 4 parcelas importadas')).toBeInTheDocument()
    expect(screen.getByText('5 registros já importados')).toBeInTheDocument()
    expect(screen.getByText('Antecipações sem data ignoradas: 1')).toBeInTheDocument()
    expect(screen.getByText('Lote: batch-123')).toBeInTheDocument()
    expect(onSaved).toHaveBeenCalledOnce()
  })

  it('surfaces workbook preview and import errors', async () => {
    previewDebtWorkbook.mockRejectedValueOnce(new Error('Planilha inválida.'))
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{}}
        loading={false}
        error=""
        month="2026-10"
        onSaved={vi.fn()}
      />,
    )

    await user.upload(screen.getByLabelText('Arquivo Excel (.xlsx)'), new File(['fixture'], 'debts.xlsx'))
    await user.click(screen.getByRole('button', { name: 'Validar e pré-visualizar' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Planilha inválida.')
  })

  it('surfaces import failure without clearing the preview or selected file', async () => {
    previewDebtWorkbook.mockResolvedValueOnce({
      debtCount: 1,
      installmentCount: 2,
      undatedEarlyPaymentsSkipped: 0,
    })
    importDebtWorkbook.mockRejectedValueOnce(new Error('Falha ao importar.'))
    const user = userEvent.setup()
    render(
      <DebtManagement
        debts={[]}
        summary={{}}
        loading={false}
        error=""
        month="2026-10"
        onSaved={vi.fn()}
      />,
    )

    await user.upload(screen.getByLabelText('Arquivo Excel (.xlsx)'), new File(['fixture'], 'debts.xlsx'))
    await user.click(screen.getByRole('button', { name: 'Validar e pré-visualizar' }))
    await screen.findByText('Dívidas encontradas no arquivo')
    await user.click(screen.getByRole('button', { name: 'Confirmar importação' }))

    expect(await screen.findByRole('alert')).toHaveTextContent('Falha ao importar.')
    expect(screen.getByText('Dívidas encontradas no arquivo')).toBeInTheDocument()
    expect(screen.getByLabelText('Arquivo Excel (.xlsx)')).toHaveValue('C:\\fakepath\\debts.xlsx')
  })
})
