// Product drink iconography for /learn cards, keyed on the shared ProductKey
// axis every LearnItem carries. Mirrors src/components/templates/productArt.ts
// (kept as-is for the old /templates route until it is retired) but sized to
// what LearnCard needs: the brand mark and the name DrinkIcon uses to scale it.

import type { ComponentType, CSSProperties } from "react";
import type { ProductKey } from "@/src/data/learn/facets";
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
}

export const PRODUCT_ART: Record<ProductKey, ProductArt> = {
  "hot-chocolate": { Drink: HotChocolate, drinkName: "hot chocolate" },
  mocha: { Drink: Mocha, drinkName: "mocha" },
  fusion: { Drink: Fusion, drinkName: "fusion" },
  nitro: { Drink: Nitro, drinkName: "nitro" },
  "strawberry-shake": { Drink: StrawberryShake, drinkName: "strawberry shake" },
};
