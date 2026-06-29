import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Pricing by the Numbers | ChilliCream",
  description:
    "Nitro GraphQL platform pricing rendered as live number tickers: 1M free operations, Pay as you go at $20/mo with 5M ops included, Dedicated from $400. Start free, scale with usage, or self-host.",
  keywords: [
    "Nitro pricing",
    "ChilliCream pricing",
    "GraphQL platform pricing",
    "GraphQL plans",
    "Hot Chocolate",
    "GraphQL observability pricing",
    "schema registry pricing",
  ],
  openGraph: {
    title: "Nitro Pricing by the Numbers | ChilliCream",
    description:
      "Pricing for the Nitro GraphQL platform shown as live tickers: free on shared cloud, Pay as you go at $20/mo, Dedicated from $400 priced by volume, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

export default function PricingPreviewV8Page() {
  return <ClientPage />;
}
