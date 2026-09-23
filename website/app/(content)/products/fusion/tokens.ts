import { MONO_FONT } from "@/src/components/mocha/palette";
import { BRAND, CC, TYPE } from "@/src/components/LayeredDiagram/tokens";

/**
 * Design tokens for the Fusion page's SVG and canvas artwork, as plain JS
 * constants for the places Tailwind utilities cannot reach: `fill`, `stroke`,
 * `stopColor`, gradient stops, `style` objects and canvas contexts.
 *
 * In markup, prefer the Tailwind utilities (`bg-cc-surface`, `text-cc-ink`,
 * `text-h5`, `font-heading`) instead of these constants.
 *
 * `CC`, `BRAND` and `TYPE` are re-exported from the Layered Diagram's own
 * tokens module (a shared component cannot import a page folder), so this
 * page's imports stay unchanged.
 */
export { BRAND, CC, TYPE };

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
