import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page exports the metadata and renders
// the client subtree where the motion hooks live.
export const metadata: Metadata = {
  title: "GraphQL Support Plans, Numbered by Tier | ChilliCream",
  description:
    "GraphQL support plans for Hot Chocolate, Fusion, and Nitro, numbered as a ladder. Climb from free community Slack to enterprise SLAs with phone support.",
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans, Numbered by Tier | ChilliCream",
    description:
      "GraphQL support plans for Hot Chocolate, Fusion, and Nitro, numbered as a ladder. Climb from free community Slack to enterprise SLAs with phone support.",
  },
  robots: { index: false, follow: false },
};

export default function SupportPreviewV8Page() {
  return <ClientPage />;
}
