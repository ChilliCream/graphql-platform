// Split into a server `page.tsx` (route metadata) + a sibling client component.
// The client file holds the motion hooks (`use client`), which is incompatible
// with exporting `metadata` from the same file in the App Router.
import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

const META_DESCRIPTION =
  "GraphQL training workshops mapped as a measured route. Six tracks across Hot Chocolate, Fusion, Nitro, and Relay, run as corporate training or workshops.";

export const metadata: Metadata = {
  title: "GraphQL Training Route & Workshops | ChilliCream",
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
    title: "GraphQL Training Route & Workshops | ChilliCream",
    description: META_DESCRIPTION,
  },
  robots: { index: false, follow: false },
};

export default function TrainingPreviewV8Page() {
  return <ClientPage />;
}
