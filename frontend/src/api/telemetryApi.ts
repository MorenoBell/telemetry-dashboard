export type LapListItem = {
  id: string
}

export type TelemetryPoint = {
  time: number
  distance: number
  speed: number
  throttle: number
  brake: number
  rpm: number
  gear: number
  latitude: number
  longitude: number
}

export type ValidationError = {
  lineNumber: number
  column: string
  message: string
}

export type TelemetryLap = {
  id: string
  fileName: string
  points: TelemetryPoint[]
  validationErrors: ValidationError[]
}

export type LapSummary = {
  lapId: string
  sampleCount: number
  maxSpeed: number
  averageSpeed: number
  maxBrake: number
  averageThrottle: number
}

type ApiErrorPayload = {
  message?: string
  errors?: ValidationError[]
}

export class TelemetryApiError extends Error {
  status: number
  errors: ValidationError[]

  constructor(status: number, message: string, errors: ValidationError[] = []) {
    super(message)
    this.name = 'TelemetryApiError'
    this.status = status
    this.errors = errors
  }
}

const apiBaseUrl = 'https://localhost:7002/api/telemetry'

async function fetchJson<T>(path: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`)

  if (!response.ok) {
    const payload = (await response.json().catch(() => null)) as ApiErrorPayload | null
    throw new TelemetryApiError(
      response.status,
      payload?.message ?? 'Unable to load telemetry data.',
      payload?.errors ?? [],
    )
  }

  return (await response.json()) as T
}

export const telemetryApi = {
  getLaps: () => fetchJson<LapListItem[]>('/laps'),
  getLap: (lapId: string) => fetchJson<TelemetryLap>(`/lap/${encodeURIComponent(lapId)}`),
  getLapSummary: (lapId: string) =>
    fetchJson<LapSummary>(`/lap/${encodeURIComponent(lapId)}/summary`),
}
