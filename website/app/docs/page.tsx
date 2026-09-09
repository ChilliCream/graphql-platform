import type { ComponentType, CSSProperties } from "react";
import { PRODUCTS } from "@/src/data/products";
import { LinkCard } from "@/src/components/LinkCard";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Typography } from "@/src/design-system/Typography";
import { Fusion, FUSION_ARTWORK } from "@/src/icons/Fusion";
import { HotChocolate, HOT_CHOCOLATE_ARTWORK } from "@/src/icons/HotChocolate";
import { Mocha, MOCHA_ARTWORK } from "@/src/icons/Mocha";
import { Nitro, NITRO_ARTWORK } from "@/src/icons/Nitro";
import {
  type ProductArtworkSize,
  productArtworkStyle,
} from "@/src/icons/productArtwork";
import { Skills, SKILLS_ARTWORK } from "@/src/icons/Skills";
import {
  StrawberryShake,
  STRAWBERRY_SHAKE_ARTWORK,
} from "@/src/icons/StrawberryShake";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { createItemListNode, schemaRef } from "@/src/helpers/structuredData";

const PAGE = {
  title: "Documentation",
  description: "Documentation for the ChilliCream GraphQL Platform.",
  path: "/docs",
} as const;

export const metadata = pageMetadata(PAGE);

/**
 * Height in rem the tallest product drink (the Strawberry Shake) occupies in a
 * card's icon tile. One scale factor sizes the whole set from it, so the cups,
 * the can and the Skills logo come out proportionally smaller.
 */
const PRODUCT_ICON_SHEET_REM = 2;

interface ProductArtwork {
  readonly Icon: ComponentType<{ style?: CSSProperties }>;
  readonly artwork: ProductArtworkSize;
}

const PRODUCT_ICONS: Record<string, ProductArtwork> = {
  hotchocolate: { Icon: HotChocolate, artwork: HOT_CHOCOLATE_ARTWORK },
  fusion: { Icon: Fusion, artwork: FUSION_ARTWORK },
  strawberryshake: {
    Icon: StrawberryShake,
    artwork: STRAWBERRY_SHAKE_ARTWORK,
  },
  nitro: { Icon: Nitro, artwork: NITRO_ARTWORK },
  mocha: { Icon: Mocha, artwork: MOCHA_ARTWORK },
  skills: { Icon: Skills, artwork: SKILLS_ARTWORK },
};

/**
 * Renders one product drink at its intrinsic aspect ratio, bottom-aligned in a
 * box of the artwork sheet's height so the bases of all six line up.
 */
function ProductIcon({ Icon, artwork }: ProductArtwork) {
  return (
    <span
      className="flex items-end"
      style={{ height: `${PRODUCT_ICON_SHEET_REM}rem` }}
    >
      <Icon style={productArtworkStyle(artwork, PRODUCT_ICON_SHEET_REM)} />
    </span>
  );
}

export default function DocsIndex() {
  const productList = createItemListNode(
    PAGE.path,
    "ChilliCream product documentation",
    PRODUCTS.map((product) => ({
      name: product.title,
      description: product.description,
      url: `/docs/${product.slug}`,
      itemType: "TechArticle",
    })),
    { order: "https://schema.org/ItemListUnordered" },
  );

  return (
    <div className="px-5 py-8 sm:px-12">
      <PageStructuredData
        {...PAGE}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Documentation" }]}
        mainEntity={schemaRef(productList["@id"]!)}
        additionalNodes={[productList]}
      />
      <div className="mx-auto max-w-5xl">
        <Typography variant="h1">Documentation</Typography>

        <ul className="mt-8 grid grid-cols-1 gap-4 sm:grid-cols-2">
          {PRODUCTS.map((product) => {
            const icon = PRODUCT_ICONS[product.slug];
            return (
              <LinkCard
                key={product.slug}
                variant="icon"
                href={`/docs/${product.slug}`}
                title={product.title}
                description={product.description}
                icon={icon ? <ProductIcon {...icon} /> : undefined}
              />
            );
          })}
        </ul>
      </div>
    </div>
  );
}
