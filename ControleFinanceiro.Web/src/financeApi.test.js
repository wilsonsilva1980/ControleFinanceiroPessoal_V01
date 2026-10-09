import { afterEach, describe, expect, it, vi } from 'vitest'

afterEach(() => {
  vi.unstubAllGlobals()
  vi.resetModules()
})

describe('finance API client', () => {
  it('uses anti-forgery protection when signing in and refreshes the session token', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'pre-sign-in-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'session-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
    vi.stubGlobal('fetch', fetchMock)
    const { signInOwner } = await import('./financeApi.js')

    await signInOwner('owner', 'test-password')

    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/auth/sign-in', expect.objectContaining({
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': 'pre-sign-in-token',
      },
      body: JSON.stringify({ userName: 'owner', password: 'test-password' }),
    }))
    expect(fetchMock).toHaveBeenNthCalledWith(3, '/api/auth/csrf-token', {
      credentials: 'same-origin',
    })
  })

  it('uses the session anti-forgery token when signing out', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'session-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(null, { status: 204 }))
    vi.stubGlobal('fetch', fetchMock)
    const { signOutOwner } = await import('./financeApi.js')

    await signOutOwner()

    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/auth/sign-out', expect.objectContaining({
      method: 'POST',
      credentials: 'same-origin',
      headers: { 'X-CSRF-TOKEN': 'session-token' },
    }))
  })

  it('fetches an anti-forgery token and sends it with a finance mutation', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'csrf-test-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ id: 7 }),
        { status: 201, headers: { 'Content-Type': 'application/json' } },
      ))
    vi.stubGlobal('fetch', fetchMock)
    const { createFinanceRecord } = await import('./financeApi.js')

    const result = await createFinanceRecord('entradas', { descricao: 'Test income' })

    expect(result).toEqual({ id: 7 })
    expect(fetchMock).toHaveBeenNthCalledWith(1, '/api/auth/csrf-token', {
      credentials: 'same-origin',
    })
    expect(fetchMock).toHaveBeenNthCalledWith(2, '/api/Entradas', expect.objectContaining({
      method: 'POST',
      credentials: 'same-origin',
      headers: {
        'Content-Type': 'application/json',
        'X-CSRF-TOKEN': 'csrf-test-token',
      },
      body: JSON.stringify({ descricao: 'Test income' }),
    }))
  })

  it('sends workbook uploads with same-origin credentials, CSRF, and browser-generated multipart headers', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'upload-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ debtCount: 2, installmentCount: 8, undatedEarlyPaymentsSkipped: 1 }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ debtsInserted: 1, installmentsInserted: 4, alreadyImported: 4 }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
    vi.stubGlobal('fetch', fetchMock)
    const { previewDebtWorkbook, importDebtWorkbook } = await import('./financeApi.js')
    const workbook = new File(['synthetic workbook'], 'debts.xlsx', {
      type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
    })

    await previewDebtWorkbook(workbook)
    await importDebtWorkbook(workbook)

    for (const [url, options] of fetchMock.mock.calls.slice(1)) {
      expect(options.credentials).toBe('same-origin')
      expect(options.headers).toEqual({ 'X-CSRF-TOKEN': 'upload-token' })
      expect(options.headers).not.toHaveProperty('Content-Type')
      expect(options.body).toBeInstanceOf(FormData)
      expect(options.body.get('file')).toBe(workbook)
      expect(url).toMatch(/^\/api\/Debts\/import\//)
    }
    expect(fetchMock.mock.calls[1][0]).toBe('/api/Debts/import/preview')
    expect(fetchMock.mock.calls[2][0]).toBe('/api/Debts/import/confirm')
  })

  it('surfaces validation errors from workbook preview uploads', async () => {
    const fetchMock = vi.fn()
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ requestToken: 'upload-token' }),
        { status: 200, headers: { 'Content-Type': 'application/json' } },
      ))
      .mockResolvedValueOnce(new Response(
        JSON.stringify({ message: 'Only .xlsx workbooks are supported.' }),
        { status: 400, headers: { 'Content-Type': 'application/json' } },
      ))
    vi.stubGlobal('fetch', fetchMock)
    const { previewDebtWorkbook } = await import('./financeApi.js')

    await expect(previewDebtWorkbook(new File(['not a workbook'], 'debts.txt')))
      .rejects.toThrow('Only .xlsx workbooks are supported.')
  })
})
