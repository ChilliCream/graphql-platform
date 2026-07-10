// Mirror of the cc-* color tokens in app/globals.css @theme. Keep in sync —
// satori (next/og) cannot read the CSS @theme tokens at build time.

export const ccBg = "#0b0f1a";
export const ccInk = "#f5f1ea";
export const ccAccent = "#5eead4";
export const ccSurface = "#0c1322";

/**
 * Mirror of the doc/marketing page background (`--cc-dark-surface` in
 * globals.css): a subtle purple nebula glow over the vertical base gradient.
 * Used as the `backgroundImage` for the share cards (over {@link ccBg}).
 */
export const ccDarkSurface =
  "radial-gradient(80% 50% at 70% 0%, rgba(80, 60, 200, 0.12), transparent 60%), " +
  "linear-gradient(180deg, #0c1322 0%, #0c1322 22%, #0b1220 38%, #0a111e 58%, #09101c 78%, #08101a 100%)";

export const ccColors = {
  bg: ccBg,
  ink: ccInk,
  accent: ccAccent,
  surface: ccSurface,
} as const;
