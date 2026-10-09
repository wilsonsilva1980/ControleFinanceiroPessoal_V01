import { useState } from 'react'
import { CalendarDays, Clock3, Plus, RefreshCw } from 'lucide-react'
import { createRecurringExpense } from './financeApi.js'
import { formatCurrency, formatDate } from './finance.js'

const cadenceLabels = { Monthly: 'Mensal', Yearly: 'Anual' }

function todayDateInput() {
  const today = new Date()
  return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
}

function newRecurringExpense() {
  return {
    name: '',
    chargeType: 'Serviço',
    amount: '',
    paymentType: '',
    cadence: 'Monthly',
    initialRenewalDate: todayDateInput(),
  }
}

function RecurringExpenseForm({ onSaved }) {
  const [form, setForm] = useState(newRecurringExpense)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function update(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createRecurringExpense({ ...form, amount: Number(form.amount) })
      setForm(newRecurringExpense())
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível salvar a cobrança recorrente.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="finance-form recurring-form" onSubmit={submit}>
      <div className="form-grid">
        <label className="form-field"><span>Nome</span><input maxLength="150" name="name" onChange={update} required value={form.name} /></label>
        <label className="form-field"><span>Tipo</span><select name="chargeType" onChange={update} value={form.chargeType}><option>Serviço</option><option>Conta da casa</option></select></label>
        <label className="form-field"><span>Valor</span><input min="0.01" name="amount" onChange={update} required step="0.01" type="number" value={form.amount} /></label>
        <label className="form-field"><span>Forma de pagamento</span><input maxLength="50" name="paymentType" onChange={update} required value={form.paymentType} /></label>
        <label className="form-field"><span>Periodicidade</span><select name="cadence" onChange={update} value={form.cadence}><option value="Monthly">Mensal</option><option value="Yearly">Anual</option></select></label>
        <label className="form-field"><span>Primeira renovação</span><input name="initialRenewalDate" onChange={update} required type="date" value={form.initialRenewalDate} /></label>
      </div>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <div className="form-actions"><span>Renovações anuais usam o mês da primeira data.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Salvar cobrança'}</button></div>
    </form>
  )
}

function RecurringExpenseList({ expenses, loading, error }) {
  if (loading) return <div className="table-empty" role="status">Carregando cobranças recorrentes...</div>
  if (error) return <div className="connection-alert" role="alert">{error}</div>
  if (!expenses.length) return <div className="table-empty">Nenhuma cobrança recorrente cadastrada.</div>

  return (
    <ul className="recurring-list">
      {expenses.map((expense) => (
        <li className="recurring-row" key={expense.id}>
          <span className="resource-mark"><RefreshCw size={16} /></span>
          <span className="resource-detail"><strong>{expense.name}</strong><small>{expense.chargeType} · {expense.paymentType}</small><small>{cadenceLabels[expense.cadence] || expense.cadence} · Primeira renovação em {formatDate(expense.initialRenewalDate)}</small></span>
          <span className="resource-values"><strong>{formatCurrency(expense.amount)}</strong><small>{cadenceLabels[expense.cadence] || expense.cadence}</small></span>
        </li>
      ))}
    </ul>
  )
}

export function RecurringRenewals({ projection, loading, error }) {
  return (
    <section className="panel recurring-upcoming" aria-labelledby="recurring-upcoming-title">
      <div className="panel-heading resource-heading">
        <div><span className="section-kicker">AGENDA DO PERÍODO</span><h2 id="recurring-upcoming-title"><CalendarDays size={17} /> Próximas renovações</h2></div>
        <span className="heading-count">{String(projection?.occurrences?.length ?? 0).padStart(2, '0')}</span>
      </div>
      {loading ? <div className="table-empty" role="status">Carregando renovações...</div>
        : error ? <div className="connection-alert" role="alert">{error}</div>
          : !projection?.occurrences?.length ? <div className="table-empty">Nenhuma renovação neste período.</div>
            : <ul className="recurring-list">
              {projection.occurrences.map((occurrence) => (
                <li className="recurring-row" key={occurrence.id}>
                  <span className="resource-mark"><Clock3 size={16} /></span>
                  <span className="resource-detail"><strong>{occurrence.name}</strong><small>Renovação em {formatDate(occurrence.renewalDate)}</small></span>
                  <span className="resource-values"><strong>{formatCurrency(occurrence.amount)}</strong><small>Valor previsto</small></span>
                </li>
              ))}
            </ul>}
    </section>
  )
}

export default function RecurringExpenses({ expenses, loading, error, onSaved }) {
  return (
    <div className="page-content recurring-page">
      <section className="panel recurring-management">
        <div className="panel-heading recurring-heading"><div><span className="section-kicker">COMPROMISSOS PROGRAMADOS</span><h2>Nova cobrança recorrente</h2><p>Defina a periodicidade e a data que ancora as renovações futuras.</p></div></div>
        <RecurringExpenseForm onSaved={onSaved} />
        <RecurringExpenseList expenses={expenses} loading={loading} error={error} />
      </section>
    </div>
  )
}
