import { useState } from 'react'
import { ArrowDownLeft, ArrowUpRight, CreditCard, House, Plus, ShoppingBag } from 'lucide-react'
import { createFinanceRecord } from './financeApi.js'
import { formatCurrency, formatDate, getRecentActivities } from './finance.js'

const months = Array.from({ length: 12 }, (_, index) => new Date(2026, index, 1).toLocaleDateString('pt-BR', { month: 'long' }))
const types = [
  { key: 'entradas', label: 'Entrada', icon: ArrowDownLeft, category: 'Salário' },
  { key: 'contasCasa', label: 'Conta da casa', icon: House, category: 'Moradia' },
  { key: 'cartaoLancamentos', label: 'Cartão', icon: CreditCard, category: 'Alimentação' },
  { key: 'despesasVariaveis', label: 'Despesa variável', icon: ShoppingBag, category: 'Diversos' },
]

function today() {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

function newForm(type) {
  const date = today()
  const now = new Date()
  return {
    descricao: '', categoria: types.find((item) => item.key === type)?.category ?? '',
    dataPrevista: date, dataVencimento: date, dataCompra: date, data: date,
    valorPrevisto: '', valorRecebido: '', dataRecebimento: date,
    valorPago: '', dataPagamento: date, status: 'Pendente',
    estabelecimento: '', numeroParcela: '1', totalParcelas: '1', valorParcela: '',
    mesFatura: now.toLocaleDateString('pt-BR', { month: 'long' }), anoFatura: String(now.getFullYear()), valor: '',
  }
}

function Field({ label, name, value, onChange, type = 'text', required = true, min, step, placeholder }) {
  return <label className="form-field"><span>{label}</span><input name={name} type={type} value={value} onChange={onChange} required={required} min={min} step={step} placeholder={placeholder} /></label>
}

function RecentRegistrations({ records }) {
  const latest = getRecentActivities(records).slice(0, 5)
  return (
    <article className="panel recent-register-panel">
      <div className="panel-heading"><div><span className="section-kicker">HISTÓRICO</span><h2>Cadastros recentes</h2></div><span className="heading-count">{String(latest.length).padStart(2, '0')}</span></div>
      {!latest.length ? (
        <div className="register-empty"><span className="empty-mark"><Plus size={18} /></span><strong>Nenhum registro ainda</strong><span>Os lançamentos cadastrados aparecem aqui.</span></div>
      ) : (
        <ul className="recent-register-list">
          {latest.map((item) => <li key={item.key}><span className={`activity-mark ${item.direction}`}>{item.direction === 'income' ? <ArrowDownLeft size={15} /> : <ArrowUpRight size={15} />}</span><span className="recent-register-detail"><strong>{item.description}</strong><small>{item.kind} · {formatDate(item.date)}</small></span><span className={`recent-register-amount amount-${item.direction}`}>{item.direction === 'income' ? '+' : '-'} {formatCurrency(item.amount)}</span></li>)}
        </ul>
      )}
    </article>
  )
}

export default function RegistrationForm({ records, onSaved }) {
  const [type, setType] = useState('entradas')
  const [form, setForm] = useState(() => newForm('entradas'))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  function updateField(event) {
    const { name, value } = event.target
    setForm((current) => ({ ...current, [name]: value }))
  }

  function chooseType(value) {
    setType(value)
    setForm(newForm(value))
    setError('')
    setSuccess('')
  }

  function payload() {
    if (type === 'entradas') return {
      dataPrevista: form.dataPrevista,
      dataRecebimento: form.status === 'Recebido' ? form.dataRecebimento : null,
      descricao: form.descricao,
      categoria: form.categoria,
      valorPrevisto: Number(form.valorPrevisto),
      valorRecebido: form.status === 'Recebido' ? Number(form.valorRecebido || form.valorPrevisto) : null,
      status: form.status,
    }
    if (type === 'contasCasa') return {
      dataVencimento: form.dataVencimento,
      dataPagamento: form.status === 'Pago' ? form.dataPagamento : null,
      descricao: form.descricao,
      categoria: form.categoria,
      valorPrevisto: Number(form.valorPrevisto),
      valorPago: form.status === 'Pago' ? Number(form.valorPago || form.valorPrevisto) : null,
      status: form.status,
    }
    if (type === 'cartaoLancamentos') return {
      dataCompra: form.dataCompra,
      estabelecimento: form.estabelecimento,
      categoria: form.categoria,
      numeroParcela: Number(form.numeroParcela),
      totalParcelas: Number(form.totalParcelas),
      valorParcela: Number(form.valorParcela),
      mesFatura: form.mesFatura,
      anoFatura: Number(form.anoFatura),
    }
    return { data: form.data, descricao: form.descricao, categoria: form.categoria, valor: Number(form.valor) }
  }

  async function submitForm(event) {
    event.preventDefault()
    setSaving(true)
    setError('')
    setSuccess('')
    try {
      await createFinanceRecord(type, payload())
      setSuccess(`${types.find((item) => item.key === type)?.label} cadastrado.`)
      setForm(newForm(type))
      onSaved()
    } catch (submitError) {
      setError(submitError.message || 'Não foi possível salvar o cadastro.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="page-content registration-page">
      <section className="registration-layout">
        <article className="panel registration-panel">
          <div className="panel-heading registration-heading"><div><span className="section-kicker">NOVO MOVIMENTO</span><h2>O que você quer registrar?</h2><p>Escolha um tipo e preencha os dados do lançamento.</p></div></div>
          <div className="registration-type-grid" role="group" aria-label="Tipo de movimentação">
            {types.map(({ key, label, icon: Icon }) => <button key={key} className={`type-option ${type === key ? 'selected' : ''}`} type="button" onClick={() => chooseType(key)} aria-pressed={type === key}><Icon size={17} /><span>{label}</span></button>)}
          </div>

          <form className="finance-form" onSubmit={submitForm}>
            {(type === 'entradas' || type === 'contasCasa') && <div className="form-grid">
              <Field label="Descrição" name="descricao" value={form.descricao} onChange={updateField} placeholder={type === 'entradas' ? 'Ex.: Salário mensal' : 'Ex.: Conta de energia'} />
              <Field label="Categoria" name="categoria" value={form.categoria} onChange={updateField} placeholder="Ex.: Moradia" />
              <Field label={type === 'entradas' ? 'Data prevista' : 'Data de vencimento'} name={type === 'entradas' ? 'dataPrevista' : 'dataVencimento'} value={type === 'entradas' ? form.dataPrevista : form.dataVencimento} onChange={updateField} type="date" />
              <Field label="Valor previsto" name="valorPrevisto" value={form.valorPrevisto} onChange={updateField} type="number" min="0.01" step="0.01" placeholder="0,00" />
            </div>}

            {(type === 'entradas' || type === 'contasCasa') && <>
              <label className="form-field"><span>Status</span><select name="status" value={form.status} onChange={updateField}><option value="Pendente">Pendente</option><option value={type === 'entradas' ? 'Recebido' : 'Pago'}>{type === 'entradas' ? 'Recebido' : 'Pago'}</option></select></label>
              {type === 'entradas' && form.status === 'Recebido' && <div className="form-grid conditional-fields"><Field label="Data do recebimento" name="dataRecebimento" value={form.dataRecebimento} onChange={updateField} type="date" /><Field label="Valor recebido" name="valorRecebido" value={form.valorRecebido} onChange={updateField} type="number" min="0.01" step="0.01" required={false} placeholder="Igual ao previsto" /></div>}
              {type === 'contasCasa' && form.status === 'Pago' && <div className="form-grid conditional-fields"><Field label="Data do pagamento" name="dataPagamento" value={form.dataPagamento} onChange={updateField} type="date" /><Field label="Valor pago" name="valorPago" value={form.valorPago} onChange={updateField} type="number" min="0.01" step="0.01" required={false} placeholder="Igual ao previsto" /></div>}
            </>}

            {type === 'cartaoLancamentos' && <div className="form-grid">
              <Field label="Estabelecimento" name="estabelecimento" value={form.estabelecimento} onChange={updateField} placeholder="Ex.: Mercado Central" />
              <Field label="Categoria" name="categoria" value={form.categoria} onChange={updateField} placeholder="Ex.: Alimentação" />
              <Field label="Data da compra" name="dataCompra" value={form.dataCompra} onChange={updateField} type="date" />
              <Field label="Valor da parcela" name="valorParcela" value={form.valorParcela} onChange={updateField} type="number" min="0.01" step="0.01" placeholder="0,00" />
              <Field label="Parcela atual" name="numeroParcela" value={form.numeroParcela} onChange={updateField} type="number" min="1" step="1" />
              <Field label="Total de parcelas" name="totalParcelas" value={form.totalParcelas} onChange={updateField} type="number" min="1" step="1" />
              <label className="form-field"><span>Mês da fatura</span><select name="mesFatura" value={form.mesFatura} onChange={updateField}>{months.map((month) => <option key={month} value={month}>{month}</option>)}</select></label>
              <Field label="Ano da fatura" name="anoFatura" value={form.anoFatura} onChange={updateField} type="number" min="2020" step="1" />
            </div>}

            {type === 'despesasVariaveis' && <div className="form-grid">
              <Field label="Descrição" name="descricao" value={form.descricao} onChange={updateField} placeholder="Ex.: Almoço" />
              <Field label="Categoria" name="categoria" value={form.categoria} onChange={updateField} placeholder="Ex.: Alimentação" />
              <Field label="Data" name="data" value={form.data} onChange={updateField} type="date" />
              <Field label="Valor" name="valor" value={form.valor} onChange={updateField} type="number" min="0.01" step="0.01" placeholder="0,00" />
            </div>}

            {error && <p className="form-message form-error" role="alert">{error}</p>}
            {success && <p className="form-message form-success" role="status">{success}</p>}
            <div className="form-actions"><span>Preencha os campos obrigatórios.</span><button className="primary-button" type="submit" disabled={saving}><Plus size={17} />{saving ? 'Salvando...' : 'Salvar cadastro'}</button></div>
          </form>
        </article>
        <RecentRegistrations records={records} />
      </section>
    </div>
  )
}
