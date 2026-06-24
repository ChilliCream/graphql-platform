import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "GraphQL Advisory by ChilliCream",
  description:
    "GraphQL advisory consulting at $300 per hour, or scoped contracting, from the engineers who build Hot Chocolate, Fusion, and Nitro. Book a 60-min intro call.",
  keywords: [
    "GraphQL advisory",
    "GraphQL consulting",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    "Nitro consulting",
    "ChilliCream advisory",
  ],
  openGraph: {
    title: "GraphQL Advisory by ChilliCream",
    description:
      "GraphQL advisory consulting at $300/hour, or scoped contracting, from the engineers behind Hot Chocolate, Fusion, and Nitro. Book a 60-minute intro call.",
  },
  robots: { index: false, follow: false },
};

export default function AdvisoryPreviewV8Page() {
  return <ClientPage />;
}
