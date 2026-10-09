const monthNames = Array.from({ length: 12 }, (_, index) =>
  new Date(2026, index, 1).toLocaleDateString('pt-BR', { month: 'long' }),
)

const normalizeText = (value) => String(value ?? '')
  .normalize('NFD')
  .replace(/[\u0300-\u036f]/g, '')
  .toLocaleLowerCase('pt-BR')

const amount = (value) => Number(value ?? 0) || 0

function localDate(value) {
  const match = String(value ?? '').match(/^(\d{4})-(\d{2})-(\d{2})/)
  return match ? new Date(Number(match[1]), Number(match[2]) - 1, Number(match[3])) : null
}

function isSameMonth(value, year, monthIndex) {
  const date = localDate(value)
  return date !== null && date.getFullYear() === year && date.getMonth() === monthIndex
}

function invoiceMatches(item, year, monthIndex) {
  const monthNumber = monthNames.findIndex((name) => normalizeText(name) === normalizeText(item.mesFatura))
  return Number(item.anoFatura) === year && monthNumber === monthIndex
}

const sum = (items, selector) => items.reduce((total, item) => total + amount(selector(item)), 0)

export function currentMonthKey(date = new Date()) {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

export function parseMonthKey(value) {
  const [year, month] = value.split('-').map(Number)
  return { year, monthIndex: month - 1 }
}

export function shiftMonth(value, offset) {
  const { year, monthIndex } = parseMonthKey(value)
  return currentMonthKey(new Date(year, monthIndex + offset, 1))
}

export function formatMonthLabel(value) {
  const { year, monthIndex } = parseMonthKey(value)
  const label = new Date(year, monthIndex, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
  return label.charAt(0).toLocaleUpperCase('pt-BR') + label.slice(1)
}

export function formatCurrency(value) {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(amount(value))
}

export function formatShortCurrency(value) {
  const numeric = amount(value)
  if (Math.abs(numeric) >= 1000) return `R$ ${(numeric / 1000).toLocaleString('pt-BR', { maximumFractionDigits: 1 })} mil`
  return `R$ ${numeric.toLocaleString('pt-BR', { maximumFractionDigits: 0 })}`
}

export function formatDate(value) {
  const date = localDate(value)
  return date ? date.toLocaleDateString('pt-BR', { day: '2-digit', month: 'short' }).replace('.', '') : '—'
}

export function getMonthlySummary(records, monthKey) {
  const { year, monthIndex } = parseMonthKey(monthKey)
  const entries = (records.entradas ?? []).filter((item) => isSameMonth(item.dataPrevista, year, monthIndex))
  const bills = (records.contasCasa ?? []).filter((item) => isSameMonth(item.dataVencimento, year, monthIndex))
  const variable = (records.despesasVariaveis ?? []).filter((item) => isSameMonth(item.data, year, monthIndex))
  const card = (records.cartaoLancamentos ?? []).filter((item) => invoiceMatches(item, year, monthIndex))
  const income = sum(entries, (item) => item.valorRecebido ?? item.valorPrevisto)
  const homeExpenses = sum(bills, (item) => item.valorPago ?? item.valorPrevisto)
  const variableExpenses = sum(variable, (item) => item.valor)
  const cardExpenses = sum(card, (item) => item.valorParcela)
  const pendingIncome = entries.filter((item) => normalizeText(item.status) !== 'recebido')
  const pendingBills = bills.filter((item) => normalizeText(item.status) !== 'pago')

  return {
    income,
    expenses: homeExpenses + variableExpenses + cardExpenses,
    balance: income - homeExpenses - variableExpenses - cardExpenses,
    pending: sum(pendingIncome, (item) => item.valorPrevisto) + sum(pendingBills, (item) => item.valorPrevisto),
  }
}

export function getSixMonthTrend(records, monthKey) {
  const { year, monthIndex } = parseMonthKey(monthKey)
  return Array.from({ length: 6 }, (_, index) => {
    const date = new Date(year, monthIndex - 5 + index, 1)
    const selectedYear = date.getFullYear()
    const selectedMonth = date.getMonth()
    const entries = (records.entradas ?? []).filter((item) => isSameMonth(item.dataPrevista, selectedYear, selectedMonth))
    const bills = (records.contasCasa ?? []).filter((item) => isSameMonth(item.dataVencimento, selectedYear, selectedMonth))
    const variable = (records.despesasVariaveis ?? []).filter((item) => isSameMonth(item.data, selectedYear, selectedMonth))
    const card = (records.cartaoLancamentos ?? []).filter((item) => invoiceMatches(item, selectedYear, selectedMonth))
    return {
      label: date.toLocaleDateString('pt-BR', { month: 'short' }).replace('.', ''),
      income: sum(entries, (item) => item.valorRecebido ?? item.valorPrevisto),
      expense: sum(bills, (item) => item.valorPago ?? item.valorPrevisto)
        + sum(variable, (item) => item.valor)
        + sum(card, (item) => item.valorParcela),
    }
  })
}

export function getRecentActivities(records) {
  return [
    ...(records.entradas ?? []).map((item) => ({ key: `entrada-${item.id}`, description: item.descricao, category: item.categoria, date: item.dataRecebimento || item.dataPrevista, amount: amount(item.valorRecebido ?? item.valorPrevisto), direction: 'income', kind: 'Entrada', status: item.status })),
    ...(records.contasCasa ?? []).map((item) => ({ key: `conta-${item.id}`, description: item.descricao, category: item.categoria, date: item.dataPagamento || item.dataVencimento, amount: amount(item.valorPago ?? item.valorPrevisto), direction: 'expense', kind: 'Conta da casa', status: item.status })),
    ...(records.cartaoLancamentos ?? []).map((item) => ({ key: `cartao-${item.id}`, description: item.estabelecimento, category: item.categoria, date: item.dataCompra, amount: amount(item.valorParcela), direction: 'expense', kind: `Cartão · ${item.numeroParcela}/${item.totalParcelas}`, status: 'Fatura' })),
    ...(records.despesasVariaveis ?? []).map((item) => ({ key: `despesa-${item.id}`, description: item.descricao, category: item.categoria, date: item.data, amount: amount(item.valor), direction: 'expense', kind: 'Despesa variável', status: 'Pago' })),
  ].sort((first, second) => (localDate(second.date)?.getTime() ?? 0) - (localDate(first.date)?.getTime() ?? 0))
}

export function getUpcomingItems(records) {
  const incoming = (records.entradas ?? []).filter((item) => normalizeText(item.status) !== 'recebido').map((item) => ({ key: `entrada-${item.id}`, description: item.descricao, label: 'Entrada prevista', date: item.dataPrevista, amount: amount(item.valorPrevisto), direction: 'income' }))
  const bills = (records.contasCasa ?? []).filter((item) => normalizeText(item.status) !== 'pago').map((item) => ({ key: `conta-${item.id}`, description: item.descricao, label: 'Conta da casa', date: item.dataVencimento, amount: amount(item.valorPrevisto), direction: 'expense' }))
  return [...incoming, ...bills].filter((item) => localDate(item.date) !== null).sort((first, second) => localDate(first.date) - localDate(second.date))
}
