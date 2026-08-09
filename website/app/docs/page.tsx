import type { ComponentType } from "react";
import { PRODUCTS } from "@/src/data/products";
import { LinkCard } from "@/src/components/LinkCard";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Typography } from "@/src/design-system/Typography";
import { Fusion } from "@/src/icons/Fusion";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { Skillz } from "@/src/icons/Skillz";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { createItemListNode, schemaRef } from "@/src/helpers/structuredData";

const PAGE = {
  title: "Documentation",
  description: "Documentation for the ChilliCream GraphQL Platform.",
  path: "/docs",
} as const;

export const metadata = pageMetadata(PAGE);

type ProductIcon = ComponentType<{ className?: string }>;

const PRODUCT_ICONS: Record<string, ProductIcon> = {
  hotchocolate: HotChocolate,
  fusion: Fusion,
  strawberryshake: StrawberryShake,
  nitro: Nitro,
  mocha: Mocha,
  skillz: Skillz,
};

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
            const Icon = PRODUCT_ICONS[product.slug];
            return (
              <LinkCard
                key={product.slug}
                variant="icon"
                href={`/docs/${product.slug}`}
                title={product.title}
                description={product.description}
                icon={Icon ? <Icon className="h-8 w-8" /> : undefined}
              />
            );
          })}
        </ul>
      </div>
    </div>
  );
}
