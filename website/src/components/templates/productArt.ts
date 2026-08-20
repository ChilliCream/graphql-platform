import type { ComponentType, CSSProperties } from "react";
import type { ProductKey } from "@/src/data/templates/filters";
import { Fusion } from "@/src/icons/Fusion";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { StrawberryShake } from "@/src/icons/StrawberryShake";

type DrinkComponent = ComponentType<{
  readonly className?: string;
  readonly style?: CSSProperties;
}>;

interface ProductArt {
  readonly Drink: DrinkComponent;
  /** Product name as `DrinkIcon` expects it, for baseline scaling. */
  readonly drinkName: string;
  /** Color wash drawn behind the artwork, keyed to the drink's own colors. */
  readonly glow: string;
}

const glowFor = (rgb: string) => `radial-gradient(ellipse 85% 75% at 50% 110%, rgba(${rgb}, 0.3), transparent 70%)`;

export const PRODUCT_ART: Record<ProductKey, ProductArt> = {
  "hot-chocolate": {
    Drink: HotChocolate,
    drinkName: "hot chocolate",
    glow: glowFor("198, 96, 46"),
  },
  mocha: {
    Drink: Mocha,
    drinkName: "mocha",
    glow: glowFor("164, 100, 63"),
  },
  fusion: {
    Drink: Fusion,
    drinkName: "fusion",
    glow: "linear-gradient(115deg, rgba(242, 119, 101, 0.16), rgba(234, 189, 33, 0.09) 30%, rgba(102, 190, 119, 0.09) 50%, rgba(0, 188, 229, 0.11) 70%, rgba(169, 131, 186, 0.16))",
  },
  nitro: {
    Drink: Nitro,
    drinkName: "nitro",
    glow: glowFor("184, 120, 31"),
  },
  "strawberry-shake": {
    Drink: StrawberryShake,
    drinkName: "strawberry shake",
    glow: glowFor("229, 43, 154"),
  },
};

/** Faint drafting-dot texture shared by the artwork variants. */
export const ART_DOT_GRID = "radial-gradient(circle, rgba(245, 241, 234, 0.1) 1px, transparent 1.2px)";

/**
 * The product whose colors key a template's artwork: the first product that is
 * not Hot Chocolate (nearly every template includes it), else the first.
 */
export const accentProduct = (products: readonly ProductKey[]): ProductKey =>
  products.find((product) => product !== "hot-chocolate") ?? products[0] ?? "hot-chocolate";
