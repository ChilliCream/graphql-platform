import type { Metadata } from "next";

import { LivingIndex } from "./LivingIndex";

export const metadata: Metadata = {
  title: "Living Index | ChilliCream Resources",
  description:
    "ChilliCream resources as a living directory tree that grows as you scroll: docs, blog, videos, Slack, GitHub, contact, shop, and legal links, indexed.",
  keywords: [
    "ChilliCream resources",
    "GraphQL docs",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream community",
    "ChilliCream legal",
  ],
  openGraph: {
    title: "Living Index | ChilliCream Resources",
    description:
      "ChilliCream resources, drawn as a living directory tree. Docs, blog, videos, Slack, GitHub, contact, shop, legal.",
  },
  robots: { index: false, follow: false },
};

export default function ResourcesLivingIndexPage() {
  return <LivingIndex />;
}
