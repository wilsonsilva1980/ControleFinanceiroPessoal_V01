import { useRef, useState } from 'react'
import { CalendarDays, Check, Clock3, Plus, RotateCcw, Trash2 } from 'lucide-react'
import {
  createDebt,
  createDebtEarlyPayment,
  importDebtWorkbook,
  previewDebtWorkbook,
  updateDebtInstallmentPayment,
} from './financeApi.js'
import { formatCurrency, formatDate, formatMonthLabel } from './finance.js'

function today() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

function toDateTime(value) {
  return `${value}T00:00:00Z`
}

function Status({ value }) {
  const isPaid = value === 'Pago'
  return (
    <span className={`status-badge ${isPaid ? 'is-done' : 'is-pending'}`}>
      {isPaid ? <Check size={13} /> : <Clock3 size={13} />}
      {value}
    </span>
  )
}

function DebtForm({ onCreated }) {
  const [description, setDescription] = useState('')
  const [dueDate, setDueDate] = useState(today())
  const [originalAmount, setOriginalAmount] = useState('')
  const [creditLimit, setCreditLimit] = useState('')
  const [schedule, setSchedule] = useState([{ dueDate: today(), scheduledAmount: '' }])
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function updateSchedule(index, field, value) {
    setSchedule((current) => current.map((item, itemIndex) => (
      itemIndex === index ? { ...item, [field]: value } : item
    )))
  }

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createDebt({
        description,
        dueDate: toDateTime(dueDate),
        originalAmount: Number(originalAmount),
        creditLimit: creditLimit ? Number(creditLimit) : null,
        installments: schedule.map((item) => ({
          dueDate: toDateTime(item.dueDate),
          scheduledAmount: Number(item.scheduledAmount),
        })),
      })
      setDescription('')
      setOriginalAmount('')
      setCreditLimit('')
      setSchedule([{ dueDate: today(), scheduledAmount: '' }])
      onCreated()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível cadastrar a dívida.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <article className="panel debt-form-panel">
      <div className="panel-heading debt-form-heading">
        <div><span className="section-kicker">NOVO COMPROMISSO</span><h2>Adicionar dívida</h2></div>
      </div>
      <form className="finance-form" onSubmit={submit}>
        <div className="form-grid">
          <label className="form-field"><span>Descrição</span><input onChange={(event) => setDescription(event.target.value)} required value={description} /></label>
          <label className="form-field"><span>Vencimento final</span><input onChange={(event) => setDueDate(event.target.value)} required type="date" value={dueDate} /></label>
          <label className="form-field"><span>Valor original</span><input min="0.01" onChange={(event) => setOriginalAmount(event.target.value)} required step="0.01" type="number" value={originalAmount} /></label>
          <label className="form-field"><span>Limite de crédito</span><input min="0" onChange={(event) => setCreditLimit(event.target.value)} step="0.01" type="number" value={creditLimit} /></label>
        </div>

        <div className="debt-schedule-fields">
          <div className="debt-schedule-heading"><strong>Parcelas</strong><button className="text-action" onClick={() => setSchedule((current) => [...current, { dueDate: today(), scheduledAmount: '' }])} type="button"><Plus size={15} />Adicionar parcela</button></div>
          {schedule.map((item, index) => (
            <div className="debt-schedule-row" key={index}>
              <label className="form-field"><span>Vencimento</span><input onChange={(event) => updateSchedule(index, 'dueDate', event.target.value)} required type="date" value={item.dueDate} /></label>
              <label className="form-field"><span>Valor previsto</span><input min="0.01" onChange={(event) => updateSchedule(index, 'scheduledAmount', event.target.value)} required step="0.01" type="number" value={item.scheduledAmount} /></label>
              <button aria-label={`Remover parcela ${index + 1}`} className="icon-button debt-remove-row" disabled={schedule.length === 1} onClick={() => setSchedule((current) => current.filter((_, itemIndex) => itemIndex !== index))} title="Remover parcela" type="button"><Trash2 size={16} /></button>
            </div>
          ))}
        </div>

        {error && <p className="form-message form-error" role="alert">{error}</p>}
        <div className="form-actions"><span>Os valores pagos e restantes são calculados pelo status.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Salvar dívida'}</button></div>
      </form>
    </article>
  )
}

function DebtWorkbookImport({ onImported }) {
  const [file, setFile] = useState(null)
  const [preview, setPreview] = useState(null)
  const [result, setResult] = useState(null)
  const [working, setWorking] = useState(false)
  const [error, setError] = useState('')
  const inputRef = useRef(null)

  function selectFile(event) {
    setFile(event.target.files?.[0] ?? null)
    setPreview(null)
    setResult(null)
    setError('')
  }

  async function previewWorkbook() {
    if (!file) return

    setWorking(true)
    setError('')
    setPreview(null)
    try {
      setPreview(await previewDebtWorkbook(file))
    } catch (previewError) {
      setError(previewError.message || 'Não foi possível validar a planilha.')
    } finally {
      setWorking(false)
    }
  }

  async function confirmImport() {
    if (!file || !preview) return

    setWorking(true)
    setError('')
    try {
      setResult(await importDebtWorkbook(file))
      setFile(null)
      setPreview(null)
      if (inputRef.current) inputRef.current.value = ''
      onImported()
    } catch (importError) {
      setError(importError.message || 'Não foi possível importar a planilha.')
    } finally {
      setWorking(false)
    }
  }

  function cancelImport() {
    setFile(null)
    setPreview(null)
    setResult(null)
    setError('')
    if (inputRef.current) inputRef.current.value = ''
  }

  return (
    <article className="panel debt-workbook-panel">
      <div className="panel-heading debt-form-heading">
        <div>
          <span className="section-kicker">IMPORTAÇÃO SEGURA</span>
          <h2>Importar planilha de dívidas</h2>
          <p className="debt-workbook-description">Selecione um arquivo .xlsx de até 10 MiB para validar os cronogramas antes de qualquer importação.</p>
        </div>
      </div>
      <label className="form-field" htmlFor="debt-workbook-file">Arquivo Excel (.xlsx)</label>
      <input
        accept=".xlsx"
        className="debt-workbook-file"
        disabled={working}
        id="debt-workbook-file"
        onChange={selectFile}
        ref={inputRef}
        type="file"
      />
      {file && (
        <div className="debt-workbook-actions">
          {!preview ? (
            <button className="primary-button" disabled={working} onClick={previewWorkbook} type="button">
              {working ? 'Validando planilha...' : 'Validar e pré-visualizar'}
            </button>
          ) : (
            <button className="primary-button" disabled={working} onClick={confirmImport} type="button">
              {working ? 'Importando...' : 'Confirmar importação'}
            </button>
          )}
          <button className="text-action" disabled={working} onClick={cancelImport} type="button">Cancelar</button>
        </div>
      )}
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      {preview && (
        <div aria-live="polite" className="debt-workbook-preview">
          <p>Contagens encontradas no arquivo (não descontam registros já existentes):</p>
          <div><span>Dívidas encontradas no arquivo</span><strong>{preview.debtCount}</strong></div>
          <div><span>Parcelas encontradas no arquivo</span><strong>{preview.installmentCount}</strong></div>
          <div><span>Antecipações sem data ignoradas</span><strong>{preview.undatedEarlyPaymentsSkipped}</strong></div>
        </div>
      )}
      {result && (
        <div aria-live="polite" className="debt-workbook-result" role="status">
          <strong>Importação concluída</strong>
          <span>{result.debtsInserted} {result.debtsInserted === 1 ? 'dívida' : 'dívidas'} e {result.installmentsInserted} parcelas importadas</span>
          <span>{result.alreadyImported} registros já importados</span>
          <span>Antecipações sem data ignoradas: {result.undatedEarlyPaymentsSkipped}</span>
          {result.batchId && <span>Lote: {result.batchId}</span>}
        </div>
      )}
    </article>
  )
}

function DebtSchedule({ debt, onSaved }) {
  const [workingId, setWorkingId] = useState(null)
  const [earlyPaymentOpen, setEarlyPaymentOpen] = useState(false)
  const [paymentDate, setPaymentDate] = useState(today())
  const [paymentAmount, setPaymentAmount] = useState('')
  const [error, setError] = useState('')

  async function updateStatus(installment) {
    const nextStatus = installment.status === 'Pago' ? 'Pendente' : 'Pago'
    setWorkingId(installment.id)
    setError('')
    try {
      await updateDebtInstallmentPayment(
        installment.id,
        nextStatus,
        nextStatus === 'Pago' ? toDateTime(today()) : null,
      )
      onSaved()
    } catch (updateError) {
      setError(updateError.message || 'Não foi possível atualizar a parcela.')
    } finally {
      setWorkingId(null)
    }
  }

  async function submitEarlyPayment(event) {
    event.preventDefault()
    setError('')
    try {
      await createDebtEarlyPayment(debt.id, {
        paymentDate: toDateTime(paymentDate),
        amount: Number(paymentAmount),
      })
      setPaymentAmount('')
      setEarlyPaymentOpen(false)
      onSaved()
    } catch (paymentError) {
      setError(paymentError.message || 'Não foi possível registrar o pagamento.')
    }
  }

  return (
    <article className="panel debt-card">
      <header className="debt-card-heading">
        <div><span className="section-kicker">DÍVIDA</span><h2>{debt.description}</h2><p>Vencimento {formatDate(debt.dueDate)} · Valor original {formatCurrency(debt.originalAmount)}</p></div>
        <span className="debt-installment-count">{debt.installments.length} parcelas</span>
      </header>
      <div className="table-scroll">
        <table className="activity-table debt-table">
          <thead><tr><th>Vencimento</th><th>Valor</th><th>Pago</th><th>Restante</th><th>Status</th><th className="debt-action-column">Ação</th></tr></thead>
          <tbody>
            {debt.installments.map((installment) => (
              <tr key={installment.id}>
                <td>{formatDate(installment.dueDate)}</td>
                <td>{formatCurrency(installment.scheduledAmount)}</td>
                <td>{formatCurrency(installment.paidAmount)}</td>
                <td>{formatCurrency(installment.remainingAmount)}</td>
                <td><Status value={installment.status} /></td>
                <td className="debt-action-column"><button className="debt-status-action" disabled={workingId === installment.id} onClick={() => updateStatus(installment)} title={installment.status === 'Pago' ? 'Marcar como pendente' : 'Marcar como pago'} type="button">{installment.status === 'Pago' ? <RotateCcw size={15} /> : <Check size={15} />}<span>{installment.status === 'Pago' ? 'Reabrir' : 'Pagar'}</span></button></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      {debt.earlyPayments.length > 0 && <div className="debt-early-list"><strong>Pagamentos antecipados</strong>{debt.earlyPayments.map((payment) => <span key={payment.id}>{formatDate(payment.paymentDate)} · {formatCurrency(payment.amount)}</span>)}</div>}
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      {earlyPaymentOpen ? (
        <form className="debt-early-form" onSubmit={submitEarlyPayment}>
          <label className="form-field"><span>Data</span><input onChange={(event) => setPaymentDate(event.target.value)} required type="date" value={paymentDate} /></label>
          <label className="form-field"><span>Valor</span><input min="0.01" onChange={(event) => setPaymentAmount(event.target.value)} required step="0.01" type="number" value={paymentAmount} /></label>
          <button className="primary-button" type="submit">Registrar pagamento</button>
          <button className="text-action" onClick={() => setEarlyPaymentOpen(false)} type="button">Cancelar</button>
        </form>
      ) : (
        <button className="text-action debt-early-trigger" onClick={() => setEarlyPaymentOpen(true)} type="button"><Plus size={15} />Registrar antecipação</button>
      )}
    </article>
  )
}

export default function DebtManagement({ debts, summary, loading, error, month, onSaved }) {
  return (
    <div className="page-content debt-page">
      <section className="debt-summary-grid" aria-label="Resumo de dívidas">
        <article className="debt-summary-banner"><span><CalendarDays size={15} /> {formatMonthLabel(month)}</span><strong>{formatCurrency(summary?.scheduledAmount)}</strong><small>Parcelas previstas</small></article>
        <article className="debt-summary-value"><span>Pago</span><strong>{formatCurrency(summary?.paidAmount)}</strong></article>
        <article className="debt-summary-value"><span>Restante</span><strong>{formatCurrency(summary?.remainingAmount)}</strong></article>
        <article className="debt-summary-value"><span>Saídas no mês</span><strong>{formatCurrency(summary?.cashOutflowAmount)}</strong></article>
      </section>

      {error && <div className="connection-alert" role="alert">{error}</div>}
      <DebtWorkbookImport onImported={onSaved} />
      <DebtForm onCreated={onSaved} />
      <section className="debt-list-section">
        <div className="panel-heading"><div><span className="section-kicker">ACOMPANHAMENTO</span><h2>Parcelas e pagamentos</h2></div><span className="heading-count">{String(debts.length).padStart(2, '0')}</span></div>
        {loading ? <div className="table-empty" role="status">Atualizando dívidas...</div> : debts.length === 0 ? <div className="table-empty">Nenhuma dívida cadastrada.</div> : debts.map((debt) => <DebtSchedule debt={debt} key={debt.id} onSaved={onSaved} />)}
      </section>
    </div>
  )
}