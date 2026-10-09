import { lazy, Suspense, useEffect, useState } from 'react'
import {
  ArrowDownLeft,
  ArrowUpRight,
  CalendarDays,
  ChevronLeft,
  ChevronRight,
  CircleAlert,
  CircleCheck,
  Clock3,
  Landmark,
  LayoutDashboard,
  LogOut,
  MapPinned,
  Plus,
  ReceiptText,
  RefreshCw,
  Wallet,
} from 'lucide-react'
import RegistrationForm from './RegistrationForm.jsx'
import DebtManagement from './DebtManagement.jsx'
import { loadDebtData, loadFinanceRecords, loadFinancialResources, loadTripBudgets, signOutOwner } from './financeApi.js'
import {
  currentMonthKey,
  formatCurrency,
  formatDate,
  formatMonthLabel,
  getMonthlySummary,
  getRecentActivities,
  getSixMonthTrend,
  getUpcomingItems,
  shiftMonth,
} from './finance.js'
import './finance.css'
import SignInForm from './SignInForm.jsx'
import FinancialResources from './FinancialResources.jsx'
import RecurringExpenses, { RecurringRenewals } from './RecurringExpenses.jsx'
import TripBudgeting from './TripBudgeting.jsx'

const emptyRecords = { entradas: [], contasCasa: [], cartaoLancamentos: [], despesasVariaveis: [] }
const FinanceChart = lazy(() => import('./FinanceChart.jsx'))

function StatusBadge({ status }) {
  const value = String(status ?? '').toLocaleLowerCase('pt-BR')
  const complete = value === 'pago' || value === 'recebido'
  const invoice = value === 'fatura'
  return (
    <span className={`status-badge ${complete ? 'is-done' : invoice ? 'is-invoice' : 'is-pending'}`}>
      {complete ? <CircleCheck size={13} /> : <Clock3 size={13} />}
      {complete ? 'Concluído' : invoice ? 'Na fatura' : 'Pendente'}
    </span>
  )
}

function MetricCard({ label, value, detail, icon: Icon, tone }) {
  return (
    <article className={`metric-card metric-${tone}`}>
      <div className="metric-topline"><span>{label}</span><span className="metric-icon"><Icon size={18} /></span></div>
      <strong>{formatCurrency(value)}</strong>
      <p>{detail}</p>
    </article>
  )
}

function MonthSwitcher({ value, onChange }) {
  return (
    <div className="month-switcher" aria-label="Selecionar mês">
      <button type="button" onClick={() => onChange(shiftMonth(value, -1))} aria-label="Mês anterior"><ChevronLeft size={17} /></button>
      <span>{formatMonthLabel(value)}</span>
      <button type="button" onClick={() => onChange(shiftMonth(value, 1))} aria-label="Próximo mês"><ChevronRight size={17} /></button>
    </div>
  )
}

function ActivityTable({ activities, loading }) {
  if (loading) return <div className="table-empty" role="status">Carregando movimentações...</div>
  if (!activities.length) return <div className="table-empty">Nenhuma movimentação cadastrada ainda.</div>

  return (
    <div className="table-scroll">
      <table className="activity-table">
        <thead><tr><th>Movimentação</th><th>Categoria</th><th>Data</th><th>Status</th><th className="amount-column">Valor</th></tr></thead>
        <tbody>
          {activities.map((activity) => (
            <tr key={activity.key}>
              <td><div className="activity-name"><span className={`activity-mark ${activity.direction}`}>{activity.direction === 'income' ? <ArrowDownLeft size={16} /> : <ArrowUpRight size={16} />}</span><span><strong>{activity.description}</strong><small>{activity.kind}</small></span></div></td>
              <td>{activity.category || 'Sem categoria'}</td>
              <td>{formatDate(activity.date)}</td>
              <td><StatusBadge status={activity.status} /></td>
              <td className={`amount-column amount-${activity.direction}`}>{activity.direction === 'income' ? '+' : '-'} {formatCurrency(activity.amount)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

function Dashboard({ records, month, onMonthChange, loading, onRegister, debtSummary, recurringProjection, recurringLoading, recurringError }) {
  const ledgerSummary = getMonthlySummary(records, month)
  const expenses = debtSummary?.cashOutflowAmount ?? ledgerSummary.expenses
  const summary = { ...ledgerSummary, expenses, balance: ledgerSummary.income - expenses }
  const trend = getSixMonthTrend(records, month)
  const activities = getRecentActivities(records).slice(0, 6)
  const hasTrendData = trend.some((item) => item.income > 0 || item.expense > 0)
  const upcoming = getUpcomingItems(records).slice(0, 4)

  return (
    <div className="page-content dashboard-page">
      <section className="balance-banner">
        <div className="balance-copy">
          <span className="banner-kicker"><Wallet size={15} /> RESUMO DO MÊS</span>
          <span className="balance-caption">Saldo planejado em {formatMonthLabel(month)}</span>
          <strong className="balance-amount">{formatCurrency(summary.balance)}</strong>
          <span className="balance-explanation">Entradas previstas menos despesas registradas</span>
        </div>
        <div className="balance-side">
          <div className="balance-month"><CalendarDays size={16} /> {formatMonthLabel(month)}</div>
          <button className="banner-action" type="button" onClick={onRegister}><Plus size={17} /> Novo lançamento</button>
        </div>
      </section>

      <section className="metric-grid" aria-label="Indicadores do mês">
        <MetricCard label="Entradas" value={summary.income} detail="Previstas para o mês" icon={ArrowDownLeft} tone="income" />
        <MetricCard label="Saídas" value={summary.expenses} detail="Contas, cartão, variáveis e dívidas" icon={ArrowUpRight} tone="expense" />
        <MetricCard label="Valores em aberto" value={summary.pending} detail="Entradas e contas pendentes" icon={Clock3} tone="pending" />
      </section>

      <section className="overview-grid">
        <article className="panel chart-panel">
          <div className="panel-heading chart-heading">
            <div><span className="section-kicker">FLUXO FINANCEIRO</span><h2>Entradas e saídas</h2><p>Movimentação prevista nos últimos seis meses</p></div>
            <MonthSwitcher value={month} onChange={onMonthChange} />
          </div>
          <div className="chart-legend"><span><i className="legend-income" /> Entradas</span><span><i className="legend-expense" /> Saídas</span></div>
          {hasTrendData ? (
            <Suspense fallback={<div className="chart-empty">Carregando gráfico...</div>}>
              <FinanceChart data={trend} />
            </Suspense>
          ) : (
            <div className="chart-empty"><strong>Sem lançamentos no período</strong><span>Cadastre movimentações para acompanhar a evolução mensal.</span></div>
          )}
        </article>

        <article className="panel commitments-panel">
          <div className="panel-heading"><div><span className="section-kicker">ACOMPANHAMENTO</span><h2>Em aberto</h2></div><span className="heading-count">{String(upcoming.length).padStart(2, '0')}</span></div>
          {!upcoming.length ? (
            <div className="commitments-empty"><CircleCheck size={20} /><span>Tudo em dia por aqui.</span></div>
          ) : (
            <ul className="commitment-list">
              {upcoming.map((item) => <li key={item.key}><span className={`commitment-symbol ${item.direction}`}>{item.direction === 'income' ? <ArrowDownLeft size={16} /> : <ReceiptText size={16} />}</span><span className="commitment-detail"><strong>{item.description}</strong><small>{item.label} · {formatDate(item.date)}</small></span><strong className="commitment-amount">{formatCurrency(item.amount)}</strong></li>)}
            </ul>
          )}
          <button className="text-action" type="button" onClick={onRegister}>Cadastrar movimentação <ChevronRight size={15} /></button>
        </article>
      </section>

      <RecurringRenewals projection={recurringProjection} loading={recurringLoading} error={recurringError} />

      <section className="panel activity-panel">
        <div className="panel-heading activity-heading"><div><span className="section-kicker">REGISTROS RECENTES</span><h2>Últimas movimentações</h2></div><button className="text-action" type="button" onClick={onRegister}>Novo cadastro <Plus size={15} /></button></div>
        <ActivityTable activities={activities} loading={loading} />
      </section>
    </div>
  )
}

function App() {
  const [page, setPage] = useState('dashboard')
  const [authStatus, setAuthStatus] = useState('checking')
  const [records, setRecords] = useState(emptyRecords)
  const [debts, setDebts] = useState([])
  const [debtSummary, setDebtSummary] = useState(null)
  const [debtLoading, setDebtLoading] = useState(true)
  const [debtError, setDebtError] = useState('')
  const [bankAccounts, setBankAccounts] = useState([])
  const [creditCards, setCreditCards] = useState([])
  const [resourcesLoading, setResourcesLoading] = useState(true)
  const [resourcesError, setResourcesError] = useState('')
  const [recurringExpenses, setRecurringExpenses] = useState([])
  const [recurringProjection, setRecurringProjection] = useState(null)
  const [recurringLoading, setRecurringLoading] = useState(true)
  const [recurringError, setRecurringError] = useState('')
  const [tripBudgets, setTripBudgets] = useState([])
  const [tripsLoading, setTripsLoading] = useState(true)
  const [tripsError, setTripsError] = useState('')
  const [month, setMonth] = useState(currentMonthKey())
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [reloadKey, setReloadKey] = useState(0)

  useEffect(() => {
    let active = true
    loadFinanceRecords()
      .then((data) => {
        if (active) {
          setRecords(data)
          setError('')
          setAuthStatus('authenticated')
        }
      })
      .catch((loadError) => {
        if (!active) return
        if (loadError.status === 401) {
          setAuthStatus('anonymous')
          setError('')
        } else {
          setAuthStatus('unavailable')
          setError(loadError.message || 'Não foi possível carregar os dados financeiros.')
        }
      })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [reloadKey])

  useEffect(() => {
    if (authStatus !== 'authenticated') return undefined
    let active = true
    loadDebtData(month)
      .then((data) => {
        if (active) {
          setDebts(data.debts)
          setDebtSummary(data.summary)
          setDebtError('')
          setRecurringExpenses(data.recurringExpenses)
          setRecurringProjection(data.recurringProjection)
          setRecurringError('')
        }
      })
      .catch((loadError) => {
        if (!active) return
        if (loadError.status === 401) {
          setAuthStatus('anonymous')
        } else {
          setDebtError(loadError.message || 'Não foi possível carregar as dívidas.')
          setRecurringError(loadError.message || 'Não foi possível carregar as cobranças recorrentes.')
        }
      })
      .finally(() => {
        if (active) {
          setDebtLoading(false)
          setRecurringLoading(false)
        }
      })
    return () => { active = false }
  }, [authStatus, month, reloadKey])

  useEffect(() => {
    if (authStatus !== 'authenticated') return undefined
    let active = true
    loadFinancialResources()
      .then((data) => {
        if (active) {
          setBankAccounts(data.accounts)
          setCreditCards(data.cards)
          setResourcesError('')
        }
      })
      .catch((loadError) => {
        if (!active) return
        if (loadError.status === 401) setAuthStatus('anonymous')
        else setResourcesError(loadError.message || 'Não foi possível carregar contas e cartões.')
      })
      .finally(() => { if (active) setResourcesLoading(false) })
    return () => { active = false }
  }, [authStatus, reloadKey])

  useEffect(() => {
    if (authStatus !== 'authenticated') return undefined
    let active = true
    loadTripBudgets()
      .then((data) => {
        if (active) {
          setTripBudgets(data ?? [])
          setTripsError('')
        }
      })
      .catch((loadError) => {
        if (!active) return
        if (loadError.status === 401) setAuthStatus('anonymous')
        else setTripsError(loadError.message || 'Não foi possível carregar os orçamentos de viagem.')
      })
      .finally(() => { if (active) setTripsLoading(false) })
    return () => { active = false }
  }, [authStatus, reloadKey])

  function refreshRecords() {
    setLoading(true)
    setDebtLoading(true)
    setResourcesLoading(true)
    setRecurringLoading(true)
    setTripsLoading(true)
    setReloadKey((current) => current + 1)
  }

  function changeMonth(value) {
    setDebtLoading(true)
    setRecurringLoading(true)
    setMonth(value)
  }

  const goToRegistration = () => setPage('registration')

  async function handleSignOut() {
    try {
      await signOutOwner()
      setRecords(emptyRecords)
      setDebts([])
      setDebtSummary(null)
      setBankAccounts([])
      setCreditCards([])
      setRecurringExpenses([])
      setRecurringProjection(null)
      setTripBudgets([])
      setPage('dashboard')
      setAuthStatus('anonymous')
    } catch (signOutError) {
      if (signOutError.status === 401) {
        setAuthStatus('anonymous')
      } else {
        setError(signOutError.message || 'Não foi possível encerrar a sessão.')
      }
    }
  }

  if (authStatus === 'checking') {
    return <main className="auth-state" role="status"><span className="auth-spinner" />Verificando sessão...</main>
  }

  if (authStatus === 'anonymous') {
    return <SignInForm onSignedIn={() => { setAuthStatus('checking'); refreshRecords() }} />
  }

  if (authStatus === 'unavailable') {
    return (
      <main className="auth-state auth-unavailable">
        <CircleAlert size={22} />
        <h1>API indisponível</h1>
        <p>{error}</p>
        <button className="primary-button" type="button" onClick={refreshRecords}>Tentar novamente</button>
      </main>
    )
  }

  const pageTitle = page === 'dashboard'
    ? 'Visão geral'
    : page === 'registration'
      ? 'Novo cadastro'
      : page === 'debts'
        ? 'Dívidas'
        : page === 'resources'
          ? 'Contas e cartões'
          : page === 'recurring'
            ? 'Cobranças recorrentes'
            : 'Viagens'
  const pageDescription = page === 'dashboard'
    ? 'Seu dinheiro, organizado em um só lugar.'
    : page === 'registration'
      ? 'Registre entradas e despesas do seu dia a dia.'
      : page === 'debts'
        ? 'Parcelas programadas e pagamentos realizados.'
        : page === 'resources'
          ? 'Saldos, limites e recursos financeiros.'
            : page === 'recurring'
              ? 'Serviços e contas com renovações mensais ou anuais.'
              : 'Custos, parcelas e pagamentos planejados.'

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand"><span className="brand-symbol"><span>cf</span></span><span className="brand-name">controle<span>financeiro</span></span></div>
        <div className="nav-caption">ESPAÇO FINANCEIRO</div>
        <nav className="sidebar-nav" aria-label="Navegação principal">
          <button aria-label="Visão geral" className={`nav-link ${page === 'dashboard' ? 'active' : ''}`} title="Visão geral" type="button" onClick={() => setPage('dashboard')}><LayoutDashboard size={18} /><span>Visão geral</span></button>
          <button aria-label="Cadastros" className={`nav-link ${page === 'registration' ? 'active' : ''}`} title="Cadastros" type="button" onClick={goToRegistration}><Plus size={18} /><span>Cadastros</span></button>
          <button aria-label="Dívidas" className={`nav-link ${page === 'debts' ? 'active' : ''}`} title="Dívidas" type="button" onClick={() => setPage('debts')}><ReceiptText size={18} /><span>Dívidas</span></button>
          <button aria-label="Contas e cartões" className={`nav-link ${page === 'resources' ? 'active' : ''}`} title="Contas e cartões" type="button" onClick={() => setPage('resources')}><Landmark size={18} /><span>Contas e cartões</span></button>
          <button aria-label="Cobranças recorrentes" className={`nav-link ${page === 'recurring' ? 'active' : ''}`} title="Cobranças recorrentes" type="button" onClick={() => setPage('recurring')}><RefreshCw size={18} /><span>Recorrentes</span></button>
          <button aria-label="Viagens" className={`nav-link ${page === 'trips' ? 'active' : ''}`} title="Viagens" type="button" onClick={() => setPage('trips')}><MapPinned size={18} /><span>Viagens</span></button>
        </nav>
        <div className="sidebar-bottom">
          <div className="sidebar-tip"><span className="tip-label">PERÍODO SELECIONADO</span><strong>{formatMonthLabel(month)}</strong><span>Acompanhe seu fluxo com clareza.</span></div>
          <div className={`api-indicator ${error ? 'api-error' : loading ? 'api-loading' : 'api-ready'}`}><span className="api-dot" /><span>{error ? 'API indisponível' : loading ? 'Conectando à API' : 'API conectada'}</span></div>
        </div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div><span className="section-kicker">CONTROLE FINANCEIRO PESSOAL</span><h1>{pageTitle}</h1><p>{pageDescription}</p></div>
          <div className="topbar-actions">
            {page === 'debts' && <MonthSwitcher value={month} onChange={changeMonth} />}
            <button className={`icon-button refresh-button ${loading ? 'is-loading' : ''}`} type="button" onClick={refreshRecords} title="Atualizar dados" aria-label="Atualizar dados"><RefreshCw size={17} /></button>
            <button className="icon-button" type="button" onClick={handleSignOut} title="Sair" aria-label="Sair"><LogOut size={17} /></button>
            <button className="primary-button" type="button" onClick={goToRegistration}><Plus size={17} /><span>Adicionar registro</span></button>
          </div>
        </header>

        {error && <div className="connection-alert" role="alert"><CircleAlert size={18} /><span>{error} Verifique a API em localhost:5105 e o acesso ao banco SQL Server.</span><button type="button" onClick={refreshRecords}>Tentar novamente</button></div>}
        {page === 'dashboard'
          ? <Dashboard records={records} month={month} onMonthChange={changeMonth} loading={loading} onRegister={goToRegistration} debtSummary={debtSummary} recurringProjection={recurringProjection} recurringLoading={recurringLoading} recurringError={recurringError} />
          : page === 'registration'
            ? <RegistrationForm records={records} onSaved={refreshRecords} />
            : page === 'debts'
              ? <DebtManagement debts={debts} summary={debtSummary} loading={debtLoading} error={debtError} month={month} onSaved={refreshRecords} />
              : page === 'resources'
                ? <FinancialResources accounts={bankAccounts} cards={creditCards} loading={resourcesLoading} error={resourcesError} onSaved={refreshRecords} />
                : page === 'recurring'
                  ? <RecurringExpenses expenses={recurringExpenses} loading={recurringLoading} error={recurringError} onSaved={refreshRecords} />
                  : <TripBudgeting budgets={tripBudgets} loading={tripsLoading} error={tripsError} onSaved={refreshRecords} />}
        <footer className="page-footer"><span>Controle Financeiro</span><span>Dados sincronizados com a API</span></footer>
      </main>
    </div>
  )
}

export default App
