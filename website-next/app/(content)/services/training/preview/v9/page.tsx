// Split into a server `page.tsx` (route metadata) + a sibling client component.
// The client file holds motion hooks (`use client`), which is incompatible with
// exporting `metadata` from the same file in the App Router.
import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

const META_DESCRIPTION =
  "GraphQL training workshops in six tracks: Hot Chocolate, Fusion, Nitro, Relay, and schema design. Run them as tailored corporate training or focused workshops.";

export const metadata: Metadata = {
  title: "GraphQL Training Workshops in Six Tracks | ChilliCream",
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
    title: "GraphQL Training Workshops in Six Tracks | ChilliCream",
    description: META_DESCRIPTION,
  },
  robots: { index: false, follow: false },
};

export default function TrainingPreviewV9Page() {
  return <ClientPage />;
}
