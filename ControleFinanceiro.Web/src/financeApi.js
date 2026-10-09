const endpoints = {
  entradas: '/api/Entradas',
  contasCasa: '/api/ContasCasa',
  cartaoLancamentos: '/api/CartaoLancamentos',
  despesasVariaveis: '/api/DespesasVariaveis',
}

let antiForgeryToken = ''

async function loadAntiForgeryToken(forceRefresh = false) {
  if (antiForgeryToken && !forceRefresh) return antiForgeryToken

  const response = await fetch('/api/auth/csrf-token', { credentials: 'same-origin' })
  const result = await response.json()
  if (!response.ok) {
    const error = new Error(result?.message || 'Não foi possível iniciar uma sessão segura.')
    error.status = response.status
    throw error
  }

  antiForgeryToken = result.requestToken
  return antiForgeryToken
}

async function requestJson(url, options = {}) {
  const method = (options.method || 'GET').toUpperCase()
  const isFormData = typeof FormData !== 'undefined' && options.body instanceof FormData
  const headers = {
    ...(options.body && !isFormData ? { 'Content-Type': 'application/json' } : {}),
    ...options.headers,
  }

  if (!['GET', 'HEAD', 'OPTIONS'].includes(method)) {
    headers['X-CSRF-TOKEN'] = await loadAntiForgeryToken()
  }

  const response = await fetch(url, {
    ...options,
    method,
    credentials: 'same-origin',
    headers,
  })
  const body = await response.text()
  let result = null

  if (body) {
    try {
      result = JSON.parse(body)
    } catch {
      result = body
    }
  }

  if (!response.ok) {
    const message = typeof result === 'object' && result !== null
      ? result.message || result.detail || result.title
      : result
    const error = new Error(message || `Falha na API (${response.status}).`)
    error.status = response.status
    throw error
  }

  return result
}

export async function loadFinanceRecords() {
  const entries = Object.entries(endpoints)
  const data = await Promise.all(entries.map(([, url]) => requestJson(url)))
  return Object.fromEntries(entries.map(([key], index) => [key, data[index] ?? []]))
}

export function createFinanceRecord(type, payload) {
  const endpoint = endpoints[type]
  if (!endpoint) throw new Error('Tipo de cadastro inválido.')
  return requestJson(endpoint, { method: 'POST', body: JSON.stringify(payload) })
}

export async function signInOwner(userName, password) {
  antiForgeryToken = ''
  await requestJson('/api/auth/sign-in', {
    method: 'POST',
    body: JSON.stringify({ userName, password }),
  })
  await loadAntiForgeryToken(true)
}

export async function signOutOwner() {
  try {
    await requestJson('/api/auth/sign-out', { method: 'POST' })
  } finally {
    antiForgeryToken = ''
  }
}

export async function loadDebtData(monthKey) {
  const [year, month] = monthKey.split('-').map(Number)
  const [debts, summary] = await Promise.all([
    requestJson('/api/Debts'),
    requestJson(`/api/Debts/summary/monthly?year=${year}&month=${month}`),
  ])
  const [recurringExpenses, recurringProjection] = await Promise.all([
    requestJson('/api/RecurringExpenses'),
    requestJson(`/api/RecurringExpenses/projection/monthly?year=${year}&month=${month}`),
  ])
  return { debts: debts ?? [], summary, recurringExpenses: recurringExpenses ?? [], recurringProjection }
}

export function createDebt(payload) {
  return requestJson('/api/Debts', { method: 'POST', body: JSON.stringify(payload) })
}

function createWorkbookForm(file) {
  const form = new FormData()
  form.append('file', file)
  return form
}

export function previewDebtWorkbook(file) {
  return requestJson('/api/Debts/import/preview', {
    method: 'POST',
    body: createWorkbookForm(file),
  })
}

export function importDebtWorkbook(file) {
  return requestJson('/api/Debts/import/confirm', {
    method: 'POST',
    body: createWorkbookForm(file),
  })
}

export function updateDebtInstallmentPayment(id, status, paymentDate) {
  return requestJson(`/api/Debts/installments/${id}/payment`, {
    method: 'PUT',
    body: JSON.stringify({ status, paymentDate }),
  })
}

export function createDebtEarlyPayment(id, payload) {
  return requestJson(`/api/Debts/${id}/early-payments`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export async function loadFinancialResources() {
  const [accounts, cards] = await Promise.all([
    requestJson('/api/BankAccounts'),
    requestJson('/api/CreditCards'),
  ])
  return { accounts: accounts ?? [], cards: cards ?? [] }
}

export function createBankAccount(payload) {
  return requestJson('/api/BankAccounts', { method: 'POST', body: JSON.stringify(payload) })
}

export function createCreditCard(payload) {
  return requestJson('/api/CreditCards', { method: 'POST', body: JSON.stringify(payload) })
}

export function createRecurringExpense(payload) {
  return requestJson('/api/RecurringExpenses', { method: 'POST', body: JSON.stringify(payload) })
}

export function loadTripBudgets() {
  return requestJson('/api/TripBudgets')
}

export function createTripBudget(payload) {
  return requestJson('/api/TripBudgets', { method: 'POST', body: JSON.stringify(payload) })
}

export function createTripCostItem(budgetId, payload) {
  return requestJson(`/api/TripBudgets/${budgetId}/items`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}

export function createTripPayment(costItemId, payload) {
  return requestJson(`/api/TripBudgets/items/${costItemId}/payments`, {
    method: 'POST',
    body: JSON.stringify(payload),
  })
}
