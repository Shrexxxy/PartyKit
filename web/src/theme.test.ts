import { describe, expect, it } from 'vitest'
import { coins, plural } from './theme'

const forms = { one: 'монетка', few: 'монетки', many: 'монеток' }

describe('plural', () => {
  it.each([
    [0, 'монеток'],
    [1, 'монетка'],
    [2, 'монетки'],
    [5, 'монеток'],
    [11, 'монеток'],
    [12, 'монеток'],
    [14, 'монеток'],
    [21, 'монетка'],
    [22, 'монетки'],
    [25, 'монеток'],
    [111, 'монеток'],
    [-1, 'монетка'],
  ])('%i → %s', (n, word) => {
    expect(plural(n, forms)).toBe(word)
  })
})

describe('coins', () => {
  it('число и слово валюты темы', () => {
    expect(coins(21, forms)).toBe('21 монетка')
    expect(coins(5, forms)).toBe('5 монеток')
  })
})
