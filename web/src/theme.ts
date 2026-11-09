/** Три формы слова: 1 монетка, 2 монетки, 5 монеток. */
export interface PluralForms {
  one: string
  few: string
  many: string
}

/** Тема праздника с сервера (`/api/theme`). */
export interface Theme {
  title: string
  host: { name: string }
  currency: PluralForms & { iconUrl: string | null }
  /** Имя CSS-переменной без `--` → значение. */
  palette: Record<string, string>
}

/** Пока тема не загрузилась (или сервер не ответил) — нейтральная. */
const FALLBACK: Theme = {
  title: 'Праздник',
  host: { name: 'Именинник' },
  currency: { one: 'монетка', few: 'монетки', many: 'монеток', iconUrl: null },
  palette: {},
}

let current: Theme = FALLBACK

/** Текущая тема. Грузится до первого рендера, поэтому компоненты читают её напрямую. */
export function theme(): Theme {
  return current
}

/** Форма слова для числа по правилам русского языка. */
export function plural(n: number, forms: PluralForms): string {
  const abs = Math.abs(n)
  const last2 = abs % 100
  const last = abs % 10
  if (last2 >= 11 && last2 <= 14) {
    return forms.many
  }
  if (last === 1) {
    return forms.one
  }
  if (last >= 2 && last <= 4) {
    return forms.few
  }
  return forms.many
}

/** «5 монеток» — число и слово валюты темы. */
export function coins(n: number, forms: PluralForms = current.currency): string {
  return `${n} ${plural(n, forms)}`
}

/** Слово валюты для числа без самого числа: «монеток». */
export function coinWord(n: number): string {
  return plural(n, current.currency)
}

/** Загружает тему, ставит палитру на `:root` и заголовок вкладки. Ошибку не бросает — остаётся нейтральная тема. */
export async function loadTheme(): Promise<void> {
  try {
    const response = await fetch('/api/theme')
    if (response.ok) {
      current = (await response.json()) as Theme
    }
  } catch {
    // Сервер недоступен — экран всё равно рисуем, ошибки покажут сами запросы.
  }

  const root = document.documentElement.style
  for (const [name, value] of Object.entries(current.palette)) {
    root.setProperty(`--${name}`, value)
  }
  document.title = current.title
}
