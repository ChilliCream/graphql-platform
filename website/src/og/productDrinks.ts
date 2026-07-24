import type { ComponentType, CSSProperties } from "react";

import { Fusion } from "@/src/icons/chillicream/Fusion";
import { HotChocolate } from "@/src/icons/chillicream/HotChocolate";
import { Mocha } from "@/src/icons/chillicream/Mocha";
import { Nitro } from "@/src/icons/chillicream/Nitro";
import { Skillz } from "@/src/icons/chillicream/Skillz";
import { StrawberryShake } from "@/src/icons/chillicream/StrawberryShake";

export interface ProductDrink {
  readonly Icon: ComponentType<{ style?: CSSProperties }>;
  /** Intrinsic height / width, so an icon can keep its aspect ratio. */
  readonly aspect: number;
}

/** Maps a product slug to its drink icon. */
export const PRODUCT_DRINKS: Record<string, ProductDrink> = {
  hotchocolate: { Icon: HotChocolate, aspect: 1.424 },
  nitro: { Icon: Nitro, aspect: 1.787 },
  strawberryshake: { Icon: StrawberryShake, aspect: 1.847 },
  fusion: { Icon: Fusion, aspect: 1.424 },
  mocha: { Icon: Mocha, aspect: 1.424 },
  skillz: { Icon: Skillz, aspect: 1 },
};
