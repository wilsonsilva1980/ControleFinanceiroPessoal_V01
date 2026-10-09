import { useState } from 'react'
import { CreditCard, Landmark, Plus } from 'lucide-react'
import { createBankAccount, createCreditCard } from './financeApi.js'
import { formatCurrency } from './finance.js'

function newBankAccount() {
  return {
    bankName: '', accountName: '', accountType: 'Conta corrente',
    branchIdentifier: '', accountIdentifier: '', balance: '',
    overdraftUsed: '0', overdraftLimit: '0',
  }
}

function newCreditCard() {
  return {
    issuer: '', displayName: '', purchaseLimit: '', localCashWithdrawalLimit: '0',
    internationalCashWithdrawalLimit: '0', creditPixEnabled: false,
    billPaymentEnabled: false, dueDay: '10',
  }
}

function BankAccountForm({ onSaved }) {
  const [form, setForm] = useState(newBankAccount)
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
      await createBankAccount({
        ...form,
        balance: Number(form.balance),
        overdraftUsed: Number(form.overdraftUsed),
        overdraftLimit: Number(form.overdraftLimit),
      })
      setForm(newBankAccount())
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível salvar a conta.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="finance-form resource-form" onSubmit={submit}>
      <div className="form-grid">
        <label className="form-field"><span>Instituição</span><input name="bankName" onChange={update} required value={form.bankName} /></label>
        <label className="form-field"><span>Nome da conta</span><input name="accountName" onChange={update} required value={form.accountName} /></label>
        <label className="form-field"><span>Tipo</span><select name="accountType" onChange={update} value={form.accountType}><option>Conta corrente</option><option>Poupança</option><option>Pagamento</option><option>Outro</option></select></label>
        <label className="form-field"><span>Agência</span><input autoComplete="off" name="branchIdentifier" onChange={update} required value={form.branchIdentifier} /></label>
        <label className="form-field"><span>Conta</span><input autoComplete="off" name="accountIdentifier" onChange={update} required value={form.accountIdentifier} /></label>
        <label className="form-field"><span>Saldo</span><input name="balance" onChange={update} required step="0.01" type="number" value={form.balance} /></label>
        <label className="form-field"><span>Cheque especial utilizado</span><input min="0" name="overdraftUsed" onChange={update} required step="0.01" type="number" value={form.overdraftUsed} /></label>
        <label className="form-field"><span>Limite de cheque especial</span><input min="0" name="overdraftLimit" onChange={update} required step="0.01" type="number" value={form.overdraftLimit} /></label>
      </div>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <div className="form-actions"><span>Identificadores completos aparecem mascarados após salvar.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Salvar conta'}</button></div>
    </form>
  )
}

function CreditCardForm({ onSaved }) {
  const [form, setForm] = useState(newCreditCard)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  function update(event) {
    const { name, value, type, checked } = event.target
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : value }))
  }

  async function submit(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    try {
      await createCreditCard({
        ...form,
        purchaseLimit: Number(form.purchaseLimit),
        localCashWithdrawalLimit: Number(form.localCashWithdrawalLimit),
        internationalCashWithdrawalLimit: Number(form.internationalCashWithdrawalLimit),
        dueDay: Number(form.dueDay),
      })
      setForm(newCreditCard())
      onSaved()
    } catch (saveError) {
      setError(saveError.message || 'Não foi possível salvar o cartão.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="finance-form resource-form" onSubmit={submit}>
      <div className="form-grid">
        <label className="form-field"><span>Emissor</span><input name="issuer" onChange={update} required value={form.issuer} /></label>
        <label className="form-field"><span>Nome do cartão</span><input name="displayName" onChange={update} required value={form.displayName} /></label>
        <label className="form-field"><span>Limite de compras</span><input min="0" name="purchaseLimit" onChange={update} required step="0.01" type="number" value={form.purchaseLimit} /></label>
        <label className="form-field"><span>Saque nacional</span><input min="0" name="localCashWithdrawalLimit" onChange={update} required step="0.01" type="number" value={form.localCashWithdrawalLimit} /></label>
        <label className="form-field"><span>Saque internacional</span><input min="0" name="internationalCashWithdrawalLimit" onChange={update} required step="0.01" type="number" value={form.internationalCashWithdrawalLimit} /></label>
        <label className="form-field"><span>Dia de vencimento</span><input max="31" min="1" name="dueDay" onChange={update} required step="1" type="number" value={form.dueDay} /></label>
      </div>
      <div className="resource-toggles">
        <label><input checked={form.creditPixEnabled} name="creditPixEnabled" onChange={update} type="checkbox" /><span>Pix no crédito</span></label>
        <label><input checked={form.billPaymentEnabled} name="billPaymentEnabled" onChange={update} type="checkbox" /><span>Pagamento de contas</span></label>
      </div>
      {error && <p className="form-message form-error" role="alert">{error}</p>}
      <div className="form-actions"><span>Não informe número completo do cartão ou CVV.</span><button className="primary-button" disabled={saving} type="submit"><Plus size={16} />{saving ? 'Salvando...' : 'Salvar cartão'}</button></div>
    </form>
  )
}

export default function FinancialResources({ accounts, cards, loading, error, onSaved }) {
  const accountBalance = accounts.reduce((total, account) => total + Number(account.balance || 0), 0)
  const availableOverdraft = accounts.reduce((total, account) => total + Number(account.availableOverdraft || 0), 0)
  const totalCardLimit = cards.reduce((total, card) => total + Number(card.purchaseLimit || 0), 0)

  return (
    <div className="page-content resources-page">
      <section className="resource-metrics" aria-label="Resumo de contas e cartões">
        <article><span>Saldo registrado</span><strong>{formatCurrency(accountBalance)}</strong></article>
        <article><span>Cheque especial disponível</span><strong>{formatCurrency(availableOverdraft)}</strong></article>
        <article><span>Limites de compras</span><strong>{formatCurrency(totalCardLimit)}</strong></article>
      </section>
      {error && <div className="connection-alert" role="alert">{error}</div>}
      <section className="resource-grid">
        <article className="panel resource-panel">
          <div className="panel-heading resource-heading"><div><span className="section-kicker">INSTITUIÇÕES</span><h2><Landmark size={17} /> Contas bancárias</h2></div><span className="heading-count">{String(accounts.length).padStart(2, '0')}</span></div>
          <BankAccountForm onSaved={onSaved} />
          <div className="resource-list">
            {loading ? <div className="table-empty" role="status">Atualizando contas...</div> : accounts.length === 0 ? <div className="table-empty">Nenhuma conta cadastrada.</div> : accounts.map((account) => (
              <div className="resource-row" key={account.id}>
                <span className="resource-mark"><Landmark size={16} /></span>
                <span className="resource-detail"><strong>{account.accountName}</strong><small>{account.bankName} · {account.accountType}</small><small>Ag. {account.branchIdentifier} · Conta {account.accountIdentifier}</small></span>
                <span className="resource-values"><strong>{formatCurrency(account.balance)}</strong><small>Disponível {formatCurrency(account.availableOverdraft)}</small></span>
              </div>
            ))}
          </div>
        </article>

        <article className="panel resource-panel">
          <div className="panel-heading resource-heading"><div><span className="section-kicker">CRÉDITO</span><h2><CreditCard size={17} /> Cartões</h2></div><span className="heading-count">{String(cards.length).padStart(2, '0')}</span></div>
          <CreditCardForm onSaved={onSaved} />
          <div className="resource-list">
            {loading ? <div className="table-empty" role="status">Atualizando cartões...</div> : cards.length === 0 ? <div className="table-empty">Nenhum cartão cadastrado.</div> : cards.map((card) => (
              <div className="resource-row" key={card.id}>
                <span className="resource-mark card-mark"><CreditCard size={16} /></span>
                <span className="resource-detail"><strong>{card.displayName}</strong><small>{card.issuer} · Vence dia {card.dueDay}</small><small>{card.creditPixEnabled ? 'Pix no crédito' : 'Sem Pix no crédito'} · {card.billPaymentEnabled ? 'Paga contas' : 'Sem pagamento de contas'}</small></span>
                <span className="resource-values"><strong>{formatCurrency(card.purchaseLimit)}</strong><small>Limite de compras</small></span>
              </div>
            ))}
          </div>
        </article>
      </section>
    </div>
  )
}