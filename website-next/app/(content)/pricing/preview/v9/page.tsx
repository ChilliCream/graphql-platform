import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro GraphQL pricing, tier cascade view",
  description:
    "Nitro GraphQL pricing that cascades from a free shared cloud tier (1M operations a month) to usage-based Pay as you go at $20/mo, to a dedicated single-tenant instance priced by volume, to self-hosted on your own infrastructure.",
  keywords: [
    "Nitro GraphQL pricing",
    "ChilliCream pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate",
    "GraphQL observability pricing",
    "schema registry pricing",
  ],
  openGraph: {
    title: "Nitro GraphQL pricing, tier cascade view",
    description:
      "Plans for the Nitro GraphQL platform: free shared cloud, usage-based Pay as you go at $20/mo, a dedicated single-tenant instance from $400/mo, or self-hosted on your own infrastructure.",
  },
  robots: { index: false, follow: false },
};

export default function PricingPreviewV9Page() {
  return <ClientPage />;
}
