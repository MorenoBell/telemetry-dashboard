import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts'

type ChartPoint = {
  time: number
  value: number
}

type TelemetryChartProps = {
  title: string
  subtitle: string
  color: string
  dataKey: string
  unit: string
  data: ChartPoint[]
}

export function TelemetryChart({
  title,
  subtitle,
  color,
  dataKey,
  unit,
  data,
}: TelemetryChartProps) {
  return (
    <section className="panel chart-panel">
      <div className="section-heading">
        <p className="eyebrow">{subtitle}</p>
        <h2>{title}</h2>
      </div>

      <div className="chart-wrap">
        <ResponsiveContainer width="100%" height={260}>
          <AreaChart data={data}>
            <defs>
              <linearGradient id={`gradient-${dataKey}`} x1="0" y1="0" x2="0" y2="1">
                <stop offset="5%" stopColor={color} stopOpacity={0.45} />
                <stop offset="95%" stopColor={color} stopOpacity={0.02} />
              </linearGradient>
            </defs>
            <CartesianGrid stroke="rgba(255,255,255,0.08)" vertical={false} />
            <XAxis
              dataKey="time"
              stroke="rgba(255,255,255,0.45)"
              tickLine={false}
              axisLine={false}
              tickFormatter={(value: number) => `${value.toFixed(0)}s`}
            />
            <YAxis
              stroke="rgba(255,255,255,0.45)"
              tickLine={false}
              axisLine={false}
              width={48}
            />
            <Tooltip
              contentStyle={{
                background: '#11151f',
                border: '1px solid rgba(255,255,255,0.12)',
                borderRadius: '14px',
                color: '#f7f7f2',
              }}
              formatter={(value) => [`${Number(value).toFixed(1)} ${unit}`, title]}
              labelFormatter={(label) => `Time ${Number(label).toFixed(2)}s`}
            />
            <Area
              type="monotone"
              dataKey="value"
              stroke={color}
              strokeWidth={2.5}
              fill={`url(#gradient-${dataKey})`}
              dot={false}
              isAnimationActive={false}
            />
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </section>
  )
}
