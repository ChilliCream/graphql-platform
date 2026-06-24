import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro GraphQL pricing, tier cascade view",
  description:
    "Nitro GraphQL pricing that cascades from free shared cloud to dedicated single-tenant with 99.95% SLA and SSO, to self-hosted on your own infrastructure.",
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
      "Plans for the Nitro GraphQL platform: shared cloud free tier, dedicated cloud at $400/mo with SLA and SSO, or self-hosted on your own infra.",
  },
  robots: { index: false, follow: false },
};

export default function PricingPreviewV9Page() {
  return <ClientPage />;
}
