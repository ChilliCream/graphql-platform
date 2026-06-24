// Split into a server `page.tsx` (route metadata) + a sibling client component.
// The client file holds all motion hooks (`use client`), which is incompatible
// with exporting `metadata` from the same file in the App Router.
import type { Metadata } from "next";

import TrainingPreviewV7Client from "./TrainingPreviewV7.client";

const META_DESCRIPTION =
  "GraphQL training workshops as an animated curriculum constellation. Six tracks, four levels, three formats covering Hot Chocolate, Fusion, Nitro, and Relay.";

export const metadata: Metadata = {
  title: "GraphQL Training Constellation | ChilliCream",
  description: META_DESCRIPTION,
  keywords: [
    "GraphQL training workshops",
    "GraphQL training",
    "Hot Chocolate training",
    "Fusion federation training",
    "Relay workshop",
    "ChilliCream training",
    "corporate GraphQL training",
  ],
  openGraph: {
    type: "website",
    siteName: "ChilliCream",
    title: "GraphQL Training Constellation | ChilliCream",
    description: META_DESCRIPTION,
  },
  robots: { index: false, follow: false },
};

export default function TrainingPreviewV7Page() {
  return <TrainingPreviewV7Client />;
}
