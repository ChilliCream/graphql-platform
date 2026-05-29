import Link from "next/link";
import { Typography } from "@/src/design-system/Typography";

export const metadata = {
  title: "Documentation",
  description: "Documentation for the ChilliCream GraphQL Platform.",
};

const PRODUCTS = [
  { slug: "hotchocolate", label: "Hot Chocolate" },
  { slug: "fusion", label: "Fusion" },
  { slug: "strawberryshake", label: "Strawberry Shake" },
  { slug: "nitro", label: "Nitro" },
  { slug: "mocha", label: "Mocha" },
];

export default function DocsIndex() {
  return (
    <div className="px-5 py-8 sm:px-12">
      <div className="mx-auto max-w-5xl">
        <Typography variant="h1">Documentation</Typography>
        <p className="text-stone-600">
          Pick a product to get started. More content is on its way.
        </p>
        <ul className="mt-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
          {PRODUCTS.map((p) => (
            <li key={p.slug}>
              <Link
                href={`/docs/${p.slug}`}
                className="block rounded-md border border-stone-200 p-4 text-stone-800 no-underline transition-colors hover:border-secondary-700 hover:text-secondary-700"
              >
                <div className="font-medium">{p.label}</div>
                <div className="text-sm text-stone-500">
                  /docs/{p.slug}
                </div>
              </Link>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}
