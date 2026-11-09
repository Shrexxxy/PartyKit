import { Emoji } from '../components/Emoji'

/** Отсчёт перед стартом ивента: на ТВ и телефонах — крупно «Приготовьтесь!», у ведущего — строкой с подсказкой. */
export function StartCountdown({ title, seconds, variant }: { title: string; seconds: number; variant: 'tv' | 'phone' | 'admin' }) {
  if (variant === 'admin') {
    return (
      <div className="card start-countdown admin">
        <h3>
          <Emoji e="⏱" /> «{title}» начнётся через {seconds} с
        </h3>
        <p className="muted">
          Передумал — «<Emoji e="⏹" /> Завершить» вернёт ивент в программу.
        </p>
      </div>
    )
  }

  return (
    <div className={`start-countdown ${variant}`}>
      <h1>{title}</h1>
      <div className="start-countdown-number">{seconds}</div>
      <p>Приготовьтесь!</p>
    </div>
  )
}
