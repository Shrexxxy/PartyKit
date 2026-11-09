import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router'
import { api } from '../api/http'
import { useSnapshot } from '../api/live'
import { useMe } from '../api/me'
import type { EventDto, LeaderboardRowDto } from '../api/types'
import { Avatar } from '../components/Avatar'
import { Coins } from '../components/CoinIcon'
import { eventPageState } from '../events/eventPage'
import { EventView } from '../events/EventView'
import { isDarkPhase } from '../events/hide'
import { HideDark } from '../events/HideView'
import { useWakeLock } from '../events/useWakeLock'
import { useStartCountdown } from '../events/useCountdown'
import { StartCountdown } from '../events/StartCountdown'
import { theme } from '../theme'

/** Итоги прошедшего ивента — его таблица монет. */
function EventResults({ eventId, highlightUserId, version }: { eventId: string; highlightUserId?: string; version?: string }) {
    const [rows, setRows] = useState<LeaderboardRowDto[] | null>(null)

    useEffect(() => {
        api<LeaderboardRowDto[]>(`/api/leaderboard?eventId=${eventId}`)
            .then(setRows)
            .catch(() => setRows([]))
    }, [eventId, version])

    if (rows === null) {
        return <p className="muted">Загружаю итоги…</p>
    }

    return rows.every((r) => r.coins === 0) ? (
        <p className="muted">В этом ивенте {theme().currency.many} не раздавали</p>
    ) : (
        <ol className="rows">
            {rows.map((r) => (
                <li key={r.userId} className={r.userId === highlightUserId ? 'row me' : 'row'}>
                    <span className="place">{r.place}</span>
                    <Avatar url={r.avatarUrl} login={r.login} />
                    <span className="login">{r.login}</span>
                    <Coins value={r.coins} />
                </li>
            ))}
        </ol>
    )
}

/** Страница одного ивента: идёт — сам ивент; прошёл — итоги; иначе — «ещё не начался». */
export function EventPage() {
    const { id = '' } = useParams()
    const { me } = useMe()
    const snapshot = useSnapshot()
    const [events, setEvents] = useState<EventDto[] | null>(null)
    const version = snapshot?.serverTime
    const active = snapshot?.activeEvent ?? null
    const [seen, setSeen] = useState<{ id: string; title: string } | null>(null)

    if (active?.id === id && seen?.id !== id) {
        setSeen({ id, title: active.title })
    }

    const state = eventPageState(id, active, events, seen?.id === id)

    const startsIn = useStartCountdown(state === 'live' ? active : null, snapshot?.serverTime)

    const dark = state === 'live' && isDarkPhase(active)
    // Пока ивент идёт, экран не гаснет; в темноте пряток — гаснет как обычно.
    useWakeLock(state === 'live' && !dark)

    useEffect(() => {
        api<EventDto[]>('/api/events')
            .then(setEvents)
            .catch(() => setEvents([]))
    }, [version])

    if (dark) {
        return <HideDark />
    }

    const title = state === 'live' ? active?.title : (events?.find((e) => e.id === id)?.title ?? (seen?.id === id ? seen.title : undefined))

    if (active && startsIn !== null) {
        return (
            <main className="page center">
                <div className="bunting" aria-hidden />
                <StartCountdown title={active.title} seconds={startsIn} variant="phone" />
            </main>
        )
    }

    return (
        <main className="page">
            <div className="bunting" aria-hidden />
            <Link to="/" className="back-link">
                ← На главную
            </Link>
            {title && <h1 className="event-title">{title}</h1>}
            {state === 'live' && active && snapshot && (
                <section className="card active-event">
                    <EventView event={active} serverTime={snapshot.serverTime} myId={me?.id} />
                </section>
            )}
            {state === 'finished' && (
                <>
                    <div className="stamp">Ивент закончился</div>
                    <EventResults eventId={id} highlightUserId={me?.id} version={version} />
                </>
            )}
            {(state === 'upcoming' || state === 'unknown') && <p className="muted">Этот ивент ещё не начался или такого нет.</p>}
            {state === 'loading' && <p className="muted">Подключаюсь…</p>}
        </main>
    )
}