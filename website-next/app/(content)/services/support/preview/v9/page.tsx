import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page owns the metadata and renders the
// client subtree where the motion hooks live.
export const metadata: Metadata = {
  title: "GraphQL Support Plans, Written Down | ChilliCream",
  description:
    "Read ChilliCream GraphQL support plans like a ledger: every Hot Chocolate, Fusion, and Nitro tier, response time, and SLA on the line, free Slack to enterprise.",
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans, Written Down | ChilliCream",
    description:
      "Read ChilliCream GraphQL support plans like a ledger: every Hot Chocolate, Fusion, and Nitro tier, response time, and SLA on the line, free Slack to enterprise.",
  },
  robots: { index: false, follow: false },
};

export default function SupportPreviewV9Page() {
  return <ClientPage />;
}
