import type { CSSProperties } from "react";

/** Rainbow accent gradient for a popular/highlighted card's border. */
export const RING_GRADIENT =
  "linear-gradient(140deg, #16b9e4 0%, #7c92c6 50%, #f0786a 100%)";

/**
 * Border treatment for a popular/highlighted card: the gradient paints the
 * border-box layer and an opaque surface fill paints the padding-box layer on
 * top, so the rainbow shows only on the edge and the interior stays solid.
 */
export const popularBorderStyle: CSSProperties = {
  border: "1.5px solid transparent",
  background: `linear-gradient(var(--color-cc-surface), var(--color-cc-surface)) padding-box, ${RING_GRADIENT} border-box`,
};
