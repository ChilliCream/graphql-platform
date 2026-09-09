import type { CSSProperties } from "react";

import { STRAWBERRY_SHAKE_ARTWORK } from "@/src/icons/StrawberryShake";

/**
 * Display size of a product icon in artwork-sheet units, as exported next to
 * the artwork itself (e.g. {@link "@/src/icons/Nitro"}'s `NITRO_ARTWORK`). It
 * is what the icon should occupy on the shared sheet, not necessarily its own
 * viewBox: the Skills logo (viewBox 64x64) is listed at 70x70 so it reads with
 * the same weight as the drinks cut from the sheet.
 */
export interface ProductArtworkSize {
  readonly width: number;
  readonly height: number;
}

/**
 * Height of the tallest drink on the sheet (Strawberry Shake). It defines the
 * slot a set of product icons is scaled into: the shake fills the slot, every
 * other icon is smaller by exactly the ratio of its sheet units.
 */
export const PRODUCT_ARTWORK_SHEET_HEIGHT = STRAWBERRY_SHAKE_ARTWORK.height;

const round = (value: number) => Math.round(value * 1000) / 1000;

/**
 * Sizes one product icon with the scale factor shared by the whole set: the
 * sheet's full height maps to `slotHeightRem`, so the shake fills the slot, the
 * cups and the Skills logo come out shorter, and the Nitro can comes out
 * narrower — the proportions of the start page hero. Bottom-align the results
 * and the bases line up; centre them horizontally and the narrow can shares one
 * centre line with the wider cups.
 *
 * @param artwork display size in artwork-sheet units, from the icon's own
 * module.
 * @param slotHeightRem height in rem the tallest drink should occupy.
 */
export function productArtworkStyle(
  artwork: ProductArtworkSize,
  slotHeightRem: number,
): CSSProperties {
  const remPerUnit = slotHeightRem / PRODUCT_ARTWORK_SHEET_HEIGHT;

  return {
    width: `${round(artwork.width * remPerUnit)}rem`,
    height: `${round(artwork.height * remPerUnit)}rem`,
  };
}
