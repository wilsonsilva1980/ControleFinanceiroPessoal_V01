import { useState } from 'react'
import { CircleAlert, MapPinned, Plus, ReceiptText } from 'lucide-react'
import { createTripBudget, createTripCostItem, createTripPayment } from './financeApi.js'
import { formatCurrency, formatDate } from './finance.js'

function TripBudgetForm({ onSaved }) {
  const [form, setForm] = useState({ name: '', destination: '', clpPerBrlQuote: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createTripBudget({
        ...form,
        clpPerBrlQuote: form.clpPerBrlQuote ? Number(form.clpPerBrlQuote) : null,
      })
      setForm({ name: '', destination: '', clpPerBrlQuote: '' })
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível salvar o orçamento.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="finance-form trip-form" onSubmit={submit}>
      <div className="form-grid">
        <label className="form-field"><span>Nome da viagem</span><input maxLength="150" onChange={(event) => setForm((current) => ({ ...current, name: event.target.value }))} required value={form.name} /></label>
        <label className="form-field"><span>Destino</span><input maxLength="100" onChange={(event) => setForm((current) => ({ ...current, destination: event.target.value }))} required value={form.destination} /></label>
        <label className="form-field"><span>Cotação CLP por BRL (opcional)</span><input min="0.0001" onChange={(event) => setForm((current) => ({ ...current, clpPerBrlQuote: event.target.value }))} step="0.0001" type="number" value={form.clpPerBrlQuote} /></label>
      </div>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <div className="form-actions"><span>A cotação fica registrada para este orçamento.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Criar viagem'}</button></div>
    </form>
  )
}

function newItemForm() {
  return {
    description: '',
    valueBRL: '',
    feesBRL: '0',
    valueUSD: '',
    installmentCount: '1',
    firstDueDate: new Date().toISOString().slice(0, 10),
  }
}

function addMonthsClamped(value, offset) {
  const year = value.getFullYear()
  const month = value.getMonth() + offset
  const targetYear = year + Math.floor(month / 12)
  const targetMonth = ((month % 12) + 12) % 12
  const day = Math.min(value.getDate(), new Date(targetYear, targetMonth + 1, 0).getDate())
  return new Date(targetYear, targetMonth, day)
}

function toDateOnlyIso(value) {
  return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}T00:00:00Z`
}

function TripCostItemForm({ budgetId, onSaved }) {
  const [form, setForm] = useState(newItemForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function update(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function submit(event) {
    event.preventDefault()
    const installmentCount = Number(form.installmentCount)
    const totalBRL = Number(form.valueBRL) + Number(form.feesBRL)
    const firstDate = new Date(`${form.firstDueDate}T00:00:00`)
    const baseInstallmentAmount = Math.floor((totalBRL / installmentCount) * 100) / 100
    const installments = Array.from({ length: installmentCount }, (_, index) => {
      const dueDate = addMonthsClamped(firstDate, index)
      const amount = index === installmentCount - 1
        ? Math.round((totalBRL - baseInstallmentAmount * (installmentCount - 1)) * 100) / 100
        : baseInstallmentAmount
      return { dueDate: toDateOnlyIso(dueDate), amount }
    })

    setSaving(true)
    setError('')
    try {
      await createTripCostItem(budgetId, {
        description: form.description,
        valueBRL: Number(form.valueBRL),
        feesBRL: Number(form.feesBRL),
        valueUSD: form.valueUSD ? Number(form.valueUSD) : null,
        installmentCount,
        installments,
      })
      setForm(newItemForm())
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível salvar o item da viagem.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="finance-form trip-item-form" onSubmit={submit}>
      <h3><Plus size={15} /> Adicionar custo</h3>
      <div className="form-grid">
        <label className="form-field"><span>Descrição</span><input maxLength="150" name="description" onChange={update} required value={form.description} /></label>
        <label className="form-field"><span>Valor (BRL)</span><input min="0" name="valueBRL" onChange={update} required step="0.01" type="number" value={form.valueBRL} /></label>
        <label className="form-field"><span>Taxas (BRL)</span><input min="0" name="feesBRL" onChange={update} required step="0.01" type="number" value={form.feesBRL} /></label>
        <label className="form-field"><span>Valor em USD (opcional)</span><input min="0" name="valueUSD" onChange={update} step="0.01" type="number" value={form.valueUSD} /></label>
        <label className="form-field"><span>Quantidade de parcelas</span><input min="1" name="installmentCount" onChange={update} required step="1" type="number" value={form.installmentCount} /></label>
        <label className="form-field"><span>Vencimento da 1ª parcela</span><input name="firstDueDate" onChange={update} required type="date" value={form.firstDueDate} /></label>
      </div>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <div className="form-actions"><span>As parcelas mensais são calculadas; eventuais centavos restantes vão para a última.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Adicionar item'}</button></div>
    </form>
  )
}

function TripPaymentForm({ item, onSaved }) {
  const [form, setForm] = useState({ amount: '', paymentDate: new Date().toISOString().slice(0, 10), tripInstallmentId: '' })
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createTripPayment(item.id, {
        amount: Number(form.amount),
        paymentDate: `${form.paymentDate}T00:00:00Z`,
        tripInstallmentId: form.tripInstallmentId ? Number(form.tripInstallmentId) : null,
      })
      setForm({ amount: '', paymentDate: new Date().toISOString().slice(0, 10), tripInstallmentId: '' })
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível registrar o pagamento.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="trip-payment-form" onSubmit={submit}>
      <label className="form-field"><span>Valor pago</span><input min="0.01" name="amount" onChange={(event) => setForm((current) => ({ ...current, amount: event.target.value }))} required step="0.01" type="number" value={form.amount} /></label>
      <label className="form-field"><span>Data</span><input name="paymentDate" onChange={(event) => setForm((current) => ({ ...current, paymentDate: event.target.value }))} required type="date" value={form.paymentDate} /></label>
      <label className="form-field"><span>Parcela</span><select name="tripInstallmentId" onChange={(event) => setForm((current) => ({ ...current, tripInstallmentId: event.target.value }))} value={form.tripInstallmentId}><option value="">Sem vínculo</option>{item.installments.map((installment, index) => <option key={installment.id} value={installment.id}>{index + 1}ª · {formatDate(installment.dueDate)}</option>)}</select></label>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <button className="text-action" disabled={saving} type="submit"><Plus size={14} />{saving ? 'Salvando...' : 'Registrar pagamento'}</button>
    </form>
  )
}

function TripBudgetCard({ budget, onSaved }) {
  return (
    <article className="panel trip-card">
      <div className="trip-card-heading">
        <span className="resource-mark"><MapPinned size={17} /></span>
        <div className="trip-card-title"><span className="section-kicker">{budget.destination}</span><h2>{budget.name}</h2></div>
        <span className="trip-installment-count">{budget.items.length} {budget.items.length === 1 ? 'item' : 'itens'}</span>
      </div>
      <div className="trip-summary-grid">
        <div><span>Total</span><strong>{formatCurrency(budget.totalBRL)}</strong></div>
        <div><span>Pago</span><strong>{formatCurrency(budget.paidAmount)}</strong></div>
        <div><span>Restante</span><strong>{formatCurrency(budget.remainingAmount)}</strong></div>
        {budget.clpTotal != null && <div><span>Total estimado em CLP</span><strong>{Number(budget.clpTotal).toLocaleString('pt-BR')} CLP</strong></div>}
      </div>
      {budget.items.map((item) => (
        <section className="trip-item" key={item.id}>
          <div className="trip-item-heading"><span><ReceiptText size={15} /><strong>{item.description}</strong></span><strong>{formatCurrency(item.totalBRL)}</strong></div>
          <p className="trip-item-detail">Valor {formatCurrency(item.valueBRL)} + taxas {formatCurrency(item.feesBRL)} · Pago {formatCurrency(item.paidAmount)} · Falta {formatCurrency(item.remainingAmount)}</p>
          {!!item.installments.length && <ol className="trip-installments">{item.installments.map((installment, index) => <li key={installment.id}><span>{index + 1}ª parcela · {formatDate(installment.dueDate)}</span><strong>{formatCurrency(installment.amount)}</strong></li>)}</ol>}
          {!!item.payments.length && <p className="trip-payment-history">Pagamentos: {item.payments.map((payment) => `${formatCurrency(payment.amount)} em ${formatDate(payment.paymentDate)}`).join(' · ')}</p>}
          <TripPaymentForm item={item} onSaved={onSaved} />
        </section>
      ))}
      <TripCostItemForm budgetId={budget.id} onSaved={onSaved} />
    </article>
  )
}

export default function TripBudgeting({ budgets, loading, error, onSaved }) {
  return (
    <div className="page-content trips-page">
      <section className="panel trip-create-panel">
        <div className="panel-heading trip-page-heading"><div><span className="section-kicker">PLANEJAMENTO DE VIAGEM</span><h2>Novo orçamento</h2><p>Registre custos, taxas, parcelas e pagamentos sem conversão automática de USD.</p></div></div>
        <TripBudgetForm onSaved={onSaved} />
      </section>
      {error && <div className="connection-alert" role="alert"><CircleAlert size={17} />{error}</div>}
      {loading ? <div className="table-empty" role="status">Carregando orçamentos...</div>
        : budgets.length === 0 ? <div className="table-empty">Nenhuma viagem cadastrada.</div>
          : <section className="trip-list" aria-label="Orçamentos de viagem">{budgets.map((budget) => <TripBudgetCard budget={budget} key={budget.id} onSaved={onSaved} />)}</section>}
    </div>
  )
}
