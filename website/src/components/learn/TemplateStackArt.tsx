import { DrinkIcon } from "@/src/components/DrinkIcon";
import type { ProductKey } from "@/src/data/learn/facets";
import { productLabel } from "@/src/data/learn/facets";
import { ART_DOT_GRID, PRODUCT_ART, accentProduct } from "./productArt";

interface TemplateStackArtProps {
  readonly products: readonly ProductKey[];
  /** Body height in px for a normal cup; raise it for larger panels. */
  readonly drinkBase?: number;
  readonly className?: string;
}

/**
 * Product-mix artwork: the template's product drinks on a shelf, washed in the
 * accent product's colors. The product list is the picture, so adding a
 * template needs no new art.
 */
export function TemplateStackArt({ products, drinkBase = 64, className }: TemplateStackArtProps) {
  const accent = PRODUCT_ART[accentProduct(products)];
  return (
    <div
      aria-hidden="true"
      className={`bg-cc-surface relative flex h-full w-full items-end justify-center overflow-hidden ${className ?? ""}`}
    >
      <div className="absolute inset-0" style={{ backgroundImage: ART_DOT_GRID, backgroundSize: "18px 18px" }} />
      <div className="absolute inset-0" style={{ backgroundImage: accent.glow }} />
      <div className="relative flex items-end gap-6 pb-[12%]">
        {products.slice(0, 3).map((product) => {
          const art = PRODUCT_ART[product];
          return (
            <span key={product} title={productLabel(product)}>
              <DrinkIcon
                Icon={art.Drink}
                name={art.drinkName}
                base={drinkBase}
                className="drop-shadow-[0_10px_24px_rgba(0,0,0,0.5)]"
              />
            </span>
          );
        })}
      </div>
    </div>
  );
}
