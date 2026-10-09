# Controle Financeiro Web

Frontend em React 19 e JavaScript, construído com Vite. Inclui dashboard mensal e formulários para entradas, contas da casa, compras no cartão e despesas variáveis.

## Executar

Na pasta `Projetos/ControleFinanceiro/ControleFinanceiro.Web`:

```powershell
npm install
npm run dev
```

O Vite serve a interface em `http://localhost:5173` e encaminha chamadas `/api` para `http://localhost:5105`. Inicie a API ASP.NET Core em Development antes de usar os cadastros.

## Acesso do titular

O acesso inicial é provisionado pela API uma única vez. Antes da primeira execução, configure `OwnerProvisioning__UserName` e `OwnerProvisioning__Password` no ambiente do processo da API ou em um gerenciador de segredos. A senha deve ter pelo menos 12 caracteres e conter os requisitos padrão do ASP.NET Core Identity. Não coloque credenciais em `appsettings.json`, no frontend ou no controle de versão.

O banco deve receber o schema de Identity antes da primeira execução da API. Quando já existir uma conta, a API não cria outra nem redefine a senha com valores de configuração. Remova as variáveis de provisionamento depois da criação inicial.

## Validar

```powershell
npm run lint
npm run build
```

O frontend usa a API existente; os dados não são simulados. Se a API ou o SQL Server estiver indisponível, a tela mostra o erro de conexão.
# React + Vite

This template provides a minimal setup to get React working in Vite with HMR and some Oxlint rules.

Currently, two official plugins are available:

- [@vitejs/plugin-react](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react) uses [Oxc](https://oxc.rs)
- [@vitejs/plugin-react-swc](https://github.com/vitejs/vite-plugin-react/blob/main/packages/plugin-react-swc) uses [SWC](https://swc.rs/)

## React Compiler

The React Compiler is not enabled on this template because of its impact on dev & build performances. To add it, see [this documentation](https://react.dev/learn/react-compiler/installation).

## Expanding the Oxlint configuration

If you are developing a production application, we recommend using TypeScript with type-aware lint rules enabled. Check out the [TS template](https://github.com/vitejs/vite/tree/main/packages/create-vite/template-react-ts) for information on how to integrate TypeScript and Oxlint's TypeScript related rules in your project.
