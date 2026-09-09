import {
  type ProductArtworkComponent,
  type ProductArtworkSize,
  productArtworkStyle,
} from "@/src/icons/productArtwork";

interface ProductArtworkIconProps {
  /** The product icon component, e.g. {@link "@/src/icons/Nitro"}'s `Nitro`. */
  readonly Icon: ProductArtworkComponent;
  /**
   * Intrinsic size of that icon in artwork-sheet units, exported next to it
   * (e.g. `NITRO_ARTWORK`).
   */
  readonly artwork: ProductArtworkSize;
  /**
   * Height in rem the tallest drink on the sheet (the Strawberry Shake) should
   * occupy. It is the scale for the whole set: every other icon comes out
   * proportionally smaller, so a set rendered with one value keeps the
   * proportions of the start page hero.
   */
  readonly slotHeightRem: number;
}

/**
 * Renders one product icon at its intrinsic aspect ratio, bottom-aligned in a
 * box of the artwork sheet's height, so the bases of a set line up. The box
 * shrinks to the icon's width and is centred by its container.
 */
export function ProductArtworkIcon({
  Icon,
  artwork,
  slotHeightRem,
}: ProductArtworkIconProps) {
  return (
    <span className="flex items-end" style={{ height: `${slotHeightRem}rem` }}>
      <Icon style={productArtworkStyle(artwork, slotHeightRem)} />
    </span>
  );
}
