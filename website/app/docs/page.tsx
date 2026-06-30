import type { ComponentType } from "react";
import Link from "next/link";
import { PRODUCTS } from "@/src/data/products";
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
              <li key={product.slug}>
                <Link
                  href={`/docs/${product.slug}`}
                  className="border-cc-card-border bg-cc-card-bg/60 hover:border-cc-accent group flex h-full items-center gap-4 rounded-2xl border p-5 no-underline transition-colors"
                >
                  <span className="bg-cc-hover ring-cc-card-border flex h-14 w-14 flex-none items-center justify-center rounded-xl ring-1">
                    {Icon ? <Icon className="h-8 w-8" /> : null}
                  </span>
                  <span className="flex flex-col">
                    <span className="font-heading text-cc-heading text-lg font-semibold">
                      {product.title}
                    </span>
                    <span className="text-cc-ink-dim text-sm">
                      {product.description}
                    </span>
                  </span>
                </Link>
              </li>
            );
          })}
        </ul>
      </div>
    </div>
  );
}
