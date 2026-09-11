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
 * The website's design tokens, as plain JS constants the Fusion concept
 * prototypes can hand to SVG attributes, canvas calls and inline styles.
 *
 * Stable exports (imported by the ten concept tasks):
 * `CC` (site colour tokens as `var(--color-cc-*)` strings), `BRAND` (the
 * brand accent hexes Mocha/Nitro already use, plus `GRADIENT`), `FONTS` (the
 * three site faces) and `TYPE` (px sizes of the site type scale, for SVG
 * text).
 *
 * Nothing here invents a value: `CC` mirrors the `--color-cc-*` custom
 * properties declared in `app/globals.css`, `BRAND` and `FONTS.mono` re-export
 * `src/components/mocha/palette.ts`, and `TYPE` is the `--text-*` scale in
 * pixels. When a token changes in `globals.css` the `var()` strings below
 * follow it at runtime, including the light-theme overrides.
 *
 * In markup, prefer the Tailwind utilities (`bg-cc-surface`, `text-cc-ink`,
 * `text-h5`, `font-heading`); these constants exist for the places Tailwind
 * cannot reach - `fill`, `stroke`, `stopColor`, gradient stops, `style`
 * objects and canvas contexts.
 *
 * ## How to map a concept palette
 *
 * A concept's `palette.ts` stops being a source of colour and becomes a
 * mapping of scene role -> token. Express a concept hue by picking a token or
 * a brand accent and varying opacity, never by adding a hex:
 *
 * ```ts
 * import { BRAND, CC } from "../../brand";
 *
 * // terminal glow: the brand teal at 90%
 * export const PHOSPHOR = `color-mix(in srgb, ${BRAND.teal} 90%, transparent)`;
 * export const OPS_ROOM_BACKDROP = CC.surface; // the scene's own backdrop
 * export const GRID_LINE = CC.cardBorder; // hairlines inside the scene
 * export const LABEL = CC.navLabel; // mono eyebrows and SVG labels
 * export const ALERT = CC.danger; // semantic states keep semantic tokens
 * ```
 *
 * `color-mix` works the same on a `CC` token as on a `BRAND` hex, so a role
 * can dim a theme colour without freezing it; in markup the Tailwind
 * `/`-modifier (`bg-cc-surface/60`) does the same job.
 *
 * The page background stays `bg-cc-bg`; a concept's atmosphere lives inside
 * its bounded scene boxes.
 */

/**
 * Every `--color-cc-*` custom property from `app/globals.css`, as a
 * `var(--color-cc-<token>)` string, so a value resolves to the live theme
 * (including the light-theme overrides) instead of being frozen at build
 * time.
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
 * re-exported from `src/components/mocha/palette.ts`. These are literal hexes,
 * so they read the same in both themes - use them for artwork that carries the
 * brand, and `CC` for anything that must follow the theme.
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
 * The only three faces on the site. `heading` and `body` are theme variables
 * from `app/globals.css`; `mono` is the site mono stack (Mocha's `MONO_FONT`).
 * SVG `<text>` uses these the same way HTML does.
 */
export const FONTS = {
  heading: "var(--font-heading)",
  body: "var(--font-body)",
  mono: MONO_FONT,
} as const;

/**
 * The `--text-*` scale from `app/globals.css` in pixels, for SVG `<text>` and
 * canvas, where `rem` utilities are not available. HTML text keeps the
 * Tailwind classes (`text-h5`, `text-body`, ...) instead of these numbers.
 *
 * Rendered size is the value times the scene's own scale: an SVG drawn in a
 * `viewBox` wider than its rendered box shrinks its text. Keep SVG text at or
 * above 11px as it renders at a 375px viewport - `label` is the safe floor,
 * and `labelTight` (10) is only for a scene that renders above 1x.
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
