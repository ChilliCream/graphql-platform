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

export const CC = {
  heading: "var(--color-cc-heading)",
  ink: "var(--color-cc-ink)",
  prose: "var(--color-cc-prose)",
  inkDim: "var(--color-cc-ink-dim)",
  inkFaint: "var(--color-cc-ink-faint)",
  hover: "var(--color-cc-hover)",
  bg: "var(--color-cc-bg)",
  cardBg: "var(--color-cc-card-bg)",
  cardBorder: "var(--color-cc-card-border)",
  cardBorderHover: "var(--color-cc-card-border-hover)",
  accent: "var(--color-cc-accent)",
  accentHover: "var(--color-cc-accent-hover)",
  cta: "var(--color-cc-cta)",
  ctaHover: "var(--color-cc-cta-hover)",
  note: "var(--color-cc-note)",
  tip: "var(--color-cc-tip)",
  success: "var(--color-cc-success)",
  warning: "var(--color-cc-warning)",
  danger: "var(--color-cc-danger)",
  info: "var(--color-cc-info)",
  surface: "var(--color-cc-surface)",
  white: "var(--color-cc-white)",
  black: "var(--color-cc-black)",
  codeBg: "var(--color-cc-code-bg)",
  codeHeader: "var(--color-cc-code-header)",
  youtube: "var(--color-cc-youtube)",
  youtubeHover: "var(--color-cc-youtube-hover)",
  navText: "var(--color-cc-nav-text)",
  navLabel: "var(--color-cc-nav-label)",
} as const;

/** Literal hexes that read the same in both themes: use these for on-brand artwork, and `CC` for anything that must follow the theme. */
export const BRAND = {
  cyan: CYAN,
  teal: TEAL,
  coral: CORAL,
  coralSoft: CORAL_SOFT,
  violet: VIOLET,
  green: GREEN,
  amber: AMBER,
  slate: SLATE,
  navy: NAVY,
  /** Two stops for an SVG/CSS gradient, teal -> cyan. */
  GRADIENT: [TEAL, CYAN] as const,
} as const;

/** The `--text-*` scale in pixels, for SVG `<text>`/canvas where `rem` utilities are not available. */
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
  label: 11,
} as const;
