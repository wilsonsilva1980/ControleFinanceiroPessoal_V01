import { useState } from 'react'
import { Eye, EyeOff, KeyRound } from 'lucide-react'
import { signInOwner } from './financeApi.js'

export default function SignInForm({ onSignedIn }) {
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [showPassword, setShowPassword] = useState(false)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState('')

  async function submit(event) {
    event.preventDefault()
    setSubmitting(true)
    setError('')
    try {
      await signInOwner(userName, password)
      setPassword('')
      onSignedIn()
    } catch (signInError) {
      setError(signInError.message || 'Não foi possível entrar.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <main className="auth-screen">
      <aside className="auth-aside">
        <div className="brand">
          <span className="brand-symbol"><span>cf</span></span>
          <span className="brand-name">controle<span>financeiro</span></span>
        </div>
        <div className="auth-aside-bottom">
          <KeyRound size={20} />
          <span>ÁREA DO TITULAR</span>
        </div>
      </aside>
      <section className="auth-main" aria-labelledby="sign-in-title">
        <form className="auth-form" onSubmit={submit}>
          <span className="section-kicker">CONTROLE FINANCEIRO PESSOAL</span>
          <h1 id="sign-in-title">Acesse sua conta</h1>
          <p>Entre com as credenciais do titular.</p>

          <label className="auth-field">
            <span>Usuário</span>
            <input
              autoComplete="username"
              autoFocus
              name="username"
              onChange={(event) => setUserName(event.target.value)}
              required
              value={userName}
            />
          </label>

          <label className="auth-field">
            <span>Senha</span>
            <span className="password-input-wrap">
              <input
                autoComplete="current-password"
                name="password"
                onChange={(event) => setPassword(event.target.value)}
                required
                type={showPassword ? 'text' : 'password'}
                value={password}
              />
              <button
                aria-label={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                className="password-toggle"
                onClick={() => setShowPassword((current) => !current)}
                title={showPassword ? 'Ocultar senha' : 'Mostrar senha'}
                type="button"
              >
                {showPassword ? <EyeOff size={17} /> : <Eye size={17} />}
              </button>
            </span>
          </label>

          {error && <p className="auth-error" role="alert">{error}</p>}
          <button className="primary-button auth-submit" disabled={submitting} type="submit">
            {submitting ? 'Entrando...' : 'Entrar'}
          </button>
        </form>
      </section>
    </main>
  )
}