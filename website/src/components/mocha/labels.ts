/**
 * Label styles for the diagrams in article content: retry, build-time and
 * order saga always, the product-page visuals in their readable mode. The
 * colors are opaque so their contrast does not depend on what is behind them;
 * ratios are against the card, rgb(11,18,33).
 */

/** Primary label text, 7.3:1 on the card. Same as `cc-ink-dim`. */
export const LABEL = "#94a3b8";

/** Secondary label text, 5.5:1 on the card. */
export const LABEL_DIM = "#7d8ca6";

/** Lowercase labels and code names. */
export const LABEL_SIZE = 12;

/** Tracked uppercase labels, which read as large as `LABEL_SIZE`. */
export const CAPS_SIZE = 11;

/** Advance of one monospace glyph at `LABEL_SIZE`, for layout estimates. */
export const LABEL_CHAR_W = 7.3;

/** Advance of one tracked uppercase glyph at `CAPS_SIZE`. */
export function capsCharW(tracking: number): number {
  return CAPS_SIZE * (0.61 + tracking);
}
