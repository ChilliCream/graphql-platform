import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  MONO_FONT,
  NAVY,
  SLATE,
  TEAL,
  VIOLET,
} from "@/src/components/mocha/palette";

/**
 * Design tokens for the Fusion page's SVG and canvas artwork, as plain JS
 * constants for the places Tailwind utilities cannot reach: `fill`, `stroke`,
 * `stopColor`, gradient stops, `style` objects and canvas contexts.
 *
 * In markup, prefer the Tailwind utilities (`bg-cc-surface`, `text-cc-ink`,
 * `text-h5`, `font-heading`) instead of these constants.
 */

/**
 * Every `--color-cc-*` custom property from `app/globals.css`, as a
 * `var(--color-cc-<token>)` string.
 */
export const CC = {
  heading: "var(--color-cc-heading)",
  ink: "var(--color-cc-ink)",
  /** Long-form body prose: lighter than `inkDim`. */
  prose: "var(--color-cc-prose)",
  inkDim: "var(--color-cc-ink-dim)",
  inkFaint: "var(--color-cc-ink-faint)",
  /** Faint light wash for hover/active surfaces. */
  hover: "var(--color-cc-hover)",
  /** The page background. Scenes sit on top of it, never replace it. */
  bg: "var(--color-cc-bg)",
  cardBg: "var(--color-cc-card-bg)",
  cardBorder: "var(--color-cc-card-border)",
  cardBorderHover: "var(--color-cc-card-border-hover)",
  accent: "var(--color-cc-accent)",
  accentHover: "var(--color-cc-accent-hover)",
  /** Secondary brand CTA (the "Launch" button). */
  cta: "var(--color-cc-cta)",
  ctaHover: "var(--color-cc-cta-hover)",
  note: "var(--color-cc-note)",
  tip: "var(--color-cc-tip)",
  success: "var(--color-cc-success)",
  warning: "var(--color-cc-warning)",
  danger: "var(--color-cc-danger)",
  info: "var(--color-cc-info)",
  /** Solid brand navy surface; `cardBg` is this colour at 55%. */
  surface: "var(--color-cc-surface)",
  white: "var(--color-cc-white)",
  black: "var(--color-cc-black)",
  codeBg: "var(--color-cc-code-bg)",
  codeHeader: "var(--color-cc-code-header)",
  youtube: "var(--color-cc-youtube)",
  youtubeHover: "var(--color-cc-youtube-hover)",
  navText: "var(--color-cc-nav-text)",
  /** Mono eyebrows and small labels. */
  navLabel: "var(--color-cc-nav-label)",
} as const;

/**
 * The brand accent set Mocha, Nitro and the federation page already use,
 * re-exported from `src/components/mocha/palette.ts`. These are literal
 * hexes that read the same in both themes: use them for artwork that carries
 * the brand, and `CC` for anything that must follow the theme.
 */
export const BRAND = {
  cyan: CYAN,
  teal: TEAL,
  coral: CORAL,
  /** The washed-out coral Mocha uses for secondary strokes. */
  coralSoft: CORAL_SOFT,
  violet: VIOLET,
  green: GREEN,
  amber: AMBER,
  slate: SLATE,
  navy: NAVY,
  /** The site gradient: teal -> cyan, as two stops for an SVG/CSS gradient. */
  GRADIENT: [TEAL, CYAN] as const,
} as const;

/**
 * The three site faces. `heading` and `body` are theme variables from
 * `app/globals.css`; `mono` is the site mono stack (Mocha's `MONO_FONT`). SVG
 * `<text>` uses these the same way HTML does.
 */
export const FONTS = {
  heading: "var(--font-heading)",
  body: "var(--font-body)",
  // globals.css defines no --font-mono; the Mocha palette value is the site's mono face.
  mono: MONO_FONT,
} as const;

/**
 * The `--text-*` scale from `app/globals.css` in pixels, for SVG `<text>` and
 * canvas, where `rem` utilities are not available. HTML text keeps the
 * Tailwind classes (`text-h5`, `text-body`, ...) instead of these numbers.
 */
export const TYPE = {
  hero: 104,
  h1: 78,
  h2: 58,
  h3: 44,
  h4: 32,
  h5: 24,
  h6: 18,
  lead: 32,
  body: 16,
  caption: 14,
  /** Mono eyebrow / SVG label; the smallest safe size at 375px. */
  label: 11,
  /** Only for scenes that render above 1x, where 10 lands at 11px or more. */
  labelTight: 10,
} as const;

/**
 * A `TYPE` size in SVG viewBox units, boosted so its rendered pixel size
 * never drops below `base` once the SVG's `viewBox` is scaled down to fit a
 * narrower box. `scale` is the SVG's own rendered-width / viewBox-width
 * ratio (`useSvgLabelScale` in `visuals/hooks.ts` measures it). At
 * `scale >= 1` this returns `base` unchanged, so a visual's rendering at its
 * native size or larger is untouched; below that, the viewBox size grows by
 * the same factor the box shrinks by, holding the rendered pixel size at
 * `base`.
 */
export function svgLabelSize(base: number, scale: number): number {
  return scale > 0 ? Math.max(base / scale, base) : base;
}

/**
 * A gap between two stacked `<text>` lines (or a line and the next row),
 * widened only by however far `size` has grown past `base`, so two labels
 * `svgLabelSize` boosted keep clear of each other. At `size === base` (today
 * at 1440) this returns `originalGap` unchanged.
 */
export function svgLabelGap(
  originalGap: number,
  size: number,
  base: number,
  factor = 1.15,
): number {
  return originalGap + Math.max(0, size - base) * factor;
}
