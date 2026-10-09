import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { formatCurrency, formatShortCurrency } from './finance.js'

export default function FinanceChart({ data }) {
  return (
    <div className="chart-wrap">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={data} margin={{ top: 10, right: 4, left: -16, bottom: 0 }} barGap={5}>
          <CartesianGrid vertical={false} stroke="#e7ece7" strokeDasharray="3 5" />
          <XAxis dataKey="label" axisLine={false} tickLine={false} tickMargin={12} />
          <YAxis axisLine={false} tickLine={false} tickFormatter={formatShortCurrency} width={54} />
          <Tooltip
            cursor={{ fill: '#f0f4ef' }}
            formatter={(value, name) => [formatCurrency(Number(value)), name]}
            contentStyle={{ border: '1px solid #e2e9e2', borderRadius: 6, boxShadow: '0 8px 24px #18251d18' }}
          />
          <Bar dataKey="income" name="Entradas" fill="#286b52" radius={[3, 3, 0, 0]} maxBarSize={34} />
          <Bar dataKey="expense" name="Saídas" fill="#e48168" radius={[3, 3, 0, 0]} maxBarSize={34} />
        </BarChart>
      </ResponsiveContainer>
    </div>
  )
}
