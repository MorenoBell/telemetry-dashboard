import type { LapSummary } from '../api/telemetryApi'

type SummaryCardsProps = {
  summary: LapSummary | null
}

function formatValue(value: number, unit: string) {
  return `${value.toFixed(1)} ${unit}`
}

export function SummaryCards({ summary }: SummaryCardsProps) {
  const items = summary
    ? [
        { label: 'Max speed', value: formatValue(summary.maxSpeed, 'km/h') },
        { label: 'Avg speed', value: formatValue(summary.averageSpeed, 'km/h') },
        { label: 'Max brake', value: formatValue(summary.maxBrake, '%') },
        { label: 'Avg throttle', value: formatValue(summary.averageThrottle, '%') },
      ]
    : [
        { label: 'Max speed', value: '--' },
        { label: 'Avg speed', value: '--' },
        { label: 'Max brake', value: '--' },
        { label: 'Avg throttle', value: '--' },
      ]

  return (
    <section className="summary-grid">
      {items.map((item) => (
        <article key={item.label} className="panel summary-card">
          <p className="summary-label">{item.label}</p>
          <p className="summary-value">{item.value}</p>
        </article>
      ))}
    </section>
  )
}
