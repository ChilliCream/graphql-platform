import type { CSSProperties } from "react";

/**
 * Style for a self-contained dark canvas inside a Light page. Re-applies the
 * Dark values of the page tokens so everything within reads as a consistent
 * Dark surface.
 */
export const DARK_STAGE_STYLE = {
  colorScheme: "dark",
  backgroundColor: "#0b0f1a",
  "--color-cc-heading": "#f5f0ea",
  "--color-cc-ink": "#f5f1ea",
  "--color-cc-prose": "rgba(245, 241, 234, 0.8)",
  "--color-cc-ink-dim": "#94a3b8",
  "--color-cc-ink-faint": "rgba(245, 241, 234, 0.16)",
  "--color-cc-hover": "rgba(245, 241, 234, 0.05)",
  "--color-cc-bg": "#0b0f1a",
  "--color-cc-card-bg": "rgba(12, 19, 34, 0.55)",
  "--color-cc-card-border": "rgba(245, 241, 234, 0.12)",
  "--color-cc-card-border-hover": "rgba(245, 241, 234, 0.32)",
  "--color-cc-accent": "#16b9e4",
  "--color-cc-accent-hover": "#4cc9ee",
  "--color-cc-solid-hover": "#fff",
  "--color-cc-control-bg": "transparent",
  "--color-cc-selected-description": "rgb(22 185 228 / 0.8)",
  "--color-cc-control-border": "rgba(245, 241, 234, 0.12)",
  "--color-cc-placeholder": "rgb(161 163 175 / 0.5)",
  "--color-cc-cta": "oklch(51.8% 0.253 323.949)",
  "--color-cc-cta-hover": "oklch(45.2% 0.211 324.591)",
  "--color-cc-note": "oklch(74.6% 0.16 232.661)",
  "--color-cc-tip": "oklch(71.4% 0.203 305.504)",
  "--color-cc-success": "oklch(76.5% 0.177 163.223)",
  "--color-cc-warning": "oklch(82.8% 0.189 84.429)",
  "--color-cc-danger": "oklch(70.4% 0.191 22.216)",
  "--color-cc-info": "oklch(70.2% 0.183 293.541)",
  "--color-cc-surface": "#0c1322",
  "--color-cc-code-bg": "#0d1117",
  "--color-cc-code-header": "#161b22",
  "--color-cc-electric": "oklch(64% 0.19 255)",
  "--color-cc-amber": "#f5ad2b",
  "--color-cc-nav-text": "#cad5e2",
  "--color-cc-nav-label": "#62748e",
} as CSSProperties;

/** Spread onto the stage element: marks it and applies {@link DARK_STAGE_STYLE}. */
export const DARK_STAGE_PROPS = {
  "data-dark-stage": "",
  style: DARK_STAGE_STYLE,
} as const;
