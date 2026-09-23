import {
  AMBER,
  CORAL,
  CORAL_SOFT,
  CYAN,
  GREEN,
  NAVY,
  SLATE,
  TEAL,
  VIOLET,
} from "@/src/components/mocha/palette";

/**
 * Design tokens the Layered Diagram draws with, for the places Tailwind
 * utilities cannot reach (`fill`, `stroke`, inline `style`): the site's
 * `--color-cc-*` custom properties, the `--text-*` type scale in pixels, and
 * the brand accent hexes from the Mocha palette. A shared component owns
 * these directly instead of importing them from a page folder;
 * `products/fusion/tokens.ts` re-exports them so the Fusion page's own
 * imports stay unchanged.
 */

/** Every `--color-cc-*` custom property from `app/globals.css`. */
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
 * The brand accent set Mocha, Nitro and the federation page already use.
 * These are literal hexes that read the same in both themes: use them for
 * artwork that carries the brand, and `CC` for anything that must follow
 * the theme.
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
 * The `--text-*` scale from `app/globals.css` in pixels, for SVG `<text>`
 * and canvas, where `rem` utilities are not available. HTML text keeps the
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
} as const;
