import { useEffect, useState } from 'react'
import { snapshotReceivedAt } from '../api/live'
import type { ActiveEventDto } from '../api/types'
import { remainingMs, startSecondsLeft } from './countdown'

/**
 * Обратный отсчёт до дедлайна по часам сервера; перерисовка 4 раза в секунду.
 */
export function useCountdown(deadline: string, serverTime: string): number {
  const [now, setNow] = useState(() => Date.now())

  useEffect(() => {
    const timer = setInterval(() => setNow(Date.now()), 250)
    return () => clearInterval(timer)
  }, [])

  return remainingMs(deadline, serverTime, snapshotReceivedAt() || now, now)
}

/** Секунды до старта ивента, пока идёт отсчёт; не отсчёт — null. */
export function useStartCountdown(event: ActiveEventDto | null | undefined, serverTime: string | undefined): number | null {
  const now = serverTime ?? new Date(0).toISOString()
  const starting = event?.status === 'Starting' && event.startedAt ? event.startedAt : null
  const ms = useCountdown(starting ?? now, now)
  return starting ? startSecondsLeft(ms) : null
}
