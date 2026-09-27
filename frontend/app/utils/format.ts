// Display helpers. Auto-imported by Nuxt (utils/ folder).

/** Short relative time for timestamps: "now", "5m", "3h", "2d", "4w", then a date. */
export function timeAgo(isoDate: string): string {
  const seconds = Math.max(0, (Date.now() - new Date(isoDate).getTime()) / 1000)

  if (seconds < 60) return 'now'
  if (seconds < 3600) return `${Math.floor(seconds / 60)}m`
  if (seconds < 86400) return `${Math.floor(seconds / 3600)}h`
  if (seconds < 604800) return `${Math.floor(seconds / 86400)}d`
  if (seconds < 4 * 604800) return `${Math.floor(seconds / 604800)}w`

  return new Date(isoDate).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
}

/** 950 → "950", 12500 → "12.5K", 3400000 → "3.4M". */
export function compactNumber(value: number): string {
  return new Intl.NumberFormat('en', { notation: 'compact', maximumFractionDigits: 1 }).format(value)
}

/** "1 like" / "2 likes". */
export function pluralize(count: number, singular: string, plural = `${singular}s`): string {
  return `${compactNumber(count)} ${count === 1 ? singular : plural}`
}
