type LapSelectorProps = {
  laps: Array<{ id: string }>
  selectedLapId: string
  onChange: (lapId: string) => void
  disabled?: boolean
}

export function LapSelector({
  laps,
  selectedLapId,
  onChange,
  disabled = false,
}: LapSelectorProps) {
  return (
    <section className="panel selector-panel">
      <div className="section-heading">
        <p className="eyebrow">Session</p>
        <h2>Select lap</h2>
      </div>

      <label className="selector-field" htmlFor="lap-select">
        <span>Telemetry file</span>
        <select
          id="lap-select"
          value={selectedLapId}
          onChange={(event) => onChange(event.target.value)}
          disabled={disabled || laps.length === 0}
        >
          {laps.length === 0 ? (
            <option value="">No laps available</option>
          ) : null}
          {laps.map((lap) => (
            <option key={lap.id} value={lap.id}>
              {lap.id}
            </option>
          ))}
        </select>
      </label>
    </section>
  )
}
