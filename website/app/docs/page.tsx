import type { ComponentType } from "react";
import { PRODUCTS } from "@/src/data/products";
import { LinkCard } from "@/src/components/LinkCard";
import { Typography } from "@/src/design-system/Typography";
import { Fusion } from "@/src/icons/Fusion";
import { HotChocolate } from "@/src/icons/HotChocolate";
import { Mocha } from "@/src/icons/Mocha";
import { Nitro } from "@/src/icons/Nitro";
import { Skillz } from "@/src/icons/Skillz";
import { StrawberryShake } from "@/src/icons/StrawberryShake";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Documentation",
  description: "Documentation for the ChilliCream GraphQL Platform.",
  path: "/docs",
});

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
  return (
    <div className="px-5 py-8 sm:px-12">
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
