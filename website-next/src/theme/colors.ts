// Mirror of the cc-* color tokens in app/globals.css @theme. Keep in sync —
// satori (next/og) cannot read the CSS @theme tokens at build time.

export const ccBg = "#0b0f1a";
export const ccInk = "#f5f1ea";
export const ccAccent = "#5eead4";
export const ccSurface = "#0c1322";

export const ccColors = {
  bg: ccBg,
  ink: ccInk,
  accent: ccAccent,
  surface: ccSurface,
} as const;
