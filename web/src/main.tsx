import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import { App } from './App'
import '@fontsource/golos-text/cyrillic-500.css'
import '@fontsource/golos-text/cyrillic-700.css'
import '@fontsource/golos-text/cyrillic-800.css'
import '@fontsource/golos-text/latin-500.css'
import '@fontsource/golos-text/latin-800.css'
import '@fontsource/unbounded/cyrillic-800.css'
import '@fontsource/unbounded/cyrillic-900.css'
import '@fontsource/unbounded/latin-900.css'
import './index.css'
import { loadTheme } from './theme'

// Тема — до первого рендера: тексты валюты и палитра нужны уже первому экрану.
void loadTheme().then(() =>
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <App />
    </StrictMode>,
  ),
)
