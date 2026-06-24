import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Pricing by the Numbers | ChilliCream",
  description:
    "Nitro GraphQL platform pricing rendered as live number tickers: 5M ops, 99.95% SLA, three environments. Start free, upgrade to dedicated, or self-host.",
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
      "Pricing for the Nitro GraphQL platform shown as live tickers: shared cloud free tier, dedicated at $400/mo with 99.95% SLA, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

export default function PricingPreviewV8Page() {
  return <ClientPage />;
}
