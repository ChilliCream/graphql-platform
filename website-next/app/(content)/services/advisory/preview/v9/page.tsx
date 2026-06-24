import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "GraphQL Advisory by ChilliCream",
  description:
    "GraphQL advisory consulting at $300 per hour, or scoped contracting, from the engineers who built Hot Chocolate, Fusion, and Nitro. Book a 60-minute call.",
  keywords: [
    "GraphQL advisory",
    "GraphQL advisory consulting",
    "GraphQL consulting",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    "Nitro consulting",
  ],
  openGraph: {
    title: "GraphQL Advisory by ChilliCream",
    description:
      "GraphQL advisory consulting at $300/hour, or scoped contracting, from the team behind Hot Chocolate, Fusion, and Nitro. Book a 60-minute call.",
  },
  robots: { index: false, follow: false },
};

export default function AdvisoryPreviewV9Page() {
  return <ClientPage />;
}
