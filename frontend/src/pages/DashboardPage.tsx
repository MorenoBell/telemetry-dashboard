import { useEffect, useState } from 'react'
import {
  TelemetryApiError,
  telemetryApi,
  type LapListItem,
  type LapSummary,
  type TelemetryLap,
} from '../api/telemetryApi'
import { LapSelector } from '../components/LapSelector'
import { SummaryCards } from '../components/SummaryCards'
import { TelemetryChart } from '../components/TelemetryChart'

type LoadState = 'idle' | 'loading' | 'ready' | 'error'

export function DashboardPage() {
  const [laps, setLaps] = useState<LapListItem[]>([])
  const [selectedLapId, setSelectedLapId] = useState('')
  const [lap, setLap] = useState<TelemetryLap | null>(null)
  const [summary, setSummary] = useState<LapSummary | null>(null)
  const [state, setState] = useState<LoadState>('idle')
  const [errorMessage, setErrorMessage] = useState('')
  const [validationDetails, setValidationDetails] = useState<string[]>([])

  useEffect(() => {
    let cancelled = false

    async function loadLaps() {
      setState('loading')
      setErrorMessage('')
      setValidationDetails([])

      try {
        const nextLaps = await telemetryApi.getLaps()
        if (cancelled) {
          return
        }

        setLaps(nextLaps)
        setSelectedLapId((current) => current || nextLaps[0]?.id || '')
        setState('ready')
      } catch (error) {
        if (cancelled) {
          return
        }

        setState('error')
        setErrorMessage('Unable to load laps from the telemetry backend.')
      }
    }

    void loadLaps()

    return () => {
      cancelled = true
    }
  }, [])

  useEffect(() => {
    if (!selectedLapId) {
      setLap(null)
      setSummary(null)
      return
    }

    let cancelled = false

    async function loadLapData() {
      setState('loading')
      setErrorMessage('')
      setValidationDetails([])

      try {
        const [nextLap, nextSummary] = await Promise.all([
          telemetryApi.getLap(selectedLapId),
          telemetryApi.getLapSummary(selectedLapId),
        ])

        if (cancelled) {
          return
        }

        setLap(nextLap)
        setSummary(nextSummary)
        setState('ready')
      } catch (error) {
        if (cancelled) {
          return
        }

        setLap(null)
        setSummary(null)
        setState('error')

        if (error instanceof TelemetryApiError) {
          setErrorMessage(error.message)
          setValidationDetails(
            error.errors.map(
              (item) => `Line ${item.lineNumber} · ${item.column} · ${item.message}`,
            ),
          )
          return
        }

        setErrorMessage('Unable to load telemetry for the selected lap.')
      }
    }

    void loadLapData()

    return () => {
      cancelled = true
    }
  }, [selectedLapId])

  const chartData = lap?.points ?? []

  return (
    <main className="dashboard-shell">
      <section className="dashboard-hero panel">
        <div>
          <p className="eyebrow">Motorsport telemetry</p>
          <h1>Race control dashboard</h1>
          <p className="hero-copy">
            Explore speed, throttle, and braking traces lap by lap with a compact
            engineering-focused view.
          </p>
        </div>
        <div className="hero-badge">
          <span className="status-dot" />
          {state === 'loading' ? 'Syncing data' : 'Telemetry online'}
        </div>
      </section>

      <LapSelector
        laps={laps}
        selectedLapId={selectedLapId}
        onChange={setSelectedLapId}
        disabled={state === 'loading'}
      />

      <SummaryCards summary={summary} />

      {errorMessage ? (
        <section className="panel message-panel error-panel">
          <h2>Data issue</h2>
          <p>{errorMessage}</p>
          {validationDetails.length > 0 ? (
            <ul className="validation-list">
              {validationDetails.map((detail) => (
                <li key={detail}>{detail}</li>
              ))}
            </ul>
          ) : null}
        </section>
      ) : null}

      {!errorMessage && chartData.length === 0 ? (
        <section className="panel message-panel">
          <h2>No telemetry yet</h2>
          <p>Add sample CSV files in the backend data folder to populate the dashboard.</p>
        </section>
      ) : null}

      {chartData.length > 0 ? (
        <section className="charts-grid">
          <TelemetryChart
            title="Speed trace"
            subtitle="Performance"
            color="#ff5b36"
            dataKey="speed"
            unit="km/h"
            data={chartData.map((point) => ({ time: point.time, value: point.speed }))}
          />
          <TelemetryChart
            title="Throttle application"
            subtitle="Driver input"
            color="#ffd447"
            dataKey="throttle"
            unit="%"
            data={chartData.map((point) => ({ time: point.time, value: point.throttle }))}
          />
          <TelemetryChart
            title="Brake pressure"
            subtitle="Driver input"
            color="#73f0c2"
            dataKey="brake"
            unit="%"
            data={chartData.map((point) => ({ time: point.time, value: point.brake }))}
          />
        </section>
      ) : null}
    </main>
  )
}
