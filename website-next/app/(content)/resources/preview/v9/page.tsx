import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Card Catalog | ChilliCream Resources",
  description:
    "The ChilliCream resources hub filed as a card catalog: docs, blog, videos, Slack, GitHub, contact, shop, and legal, each on its own browsable index card.",
  keywords: [
    "ChilliCream resources",
    "GraphQL docs",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream community",
    "ChilliCream legal",
  ],
  openGraph: {
    title: "Card Catalog | ChilliCream Resources",
    description:
      "A card-catalog resources hub: docs, blog, videos, Slack, GitHub, contact, shop, and legal, each filed on its own index card.",
  },
  robots: { index: false, follow: false },
};

export default function ResourcesCardCatalogPage() {
  return <ClientPage />;
}
