import type { Metadata } from "next";

import { SupportPreviewV7Client } from "./SupportPreviewV7Client";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page exports the metadata and renders
// the client subtree where the motion hooks live.
export const metadata: Metadata = {
  title: "GraphQL Support Plans on a Clock You Can See | ChilliCream",
  description:
    "GraphQL support plans for Hot Chocolate, Fusion, and Nitro. Watch the SLA from incident open to first engineer response, free Slack to 24 hour enterprise.",
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "GraphQL SLA",
    "enterprise GraphQL support",
    "ChilliCream support plans",
  ],
  openGraph: {
    title: "GraphQL Support Plans on a Clock You Can See | ChilliCream",
    description:
      "GraphQL support plans for Hot Chocolate, Fusion, and Nitro. Watch the SLA from incident open to first engineer response, free Slack to 24 hour enterprise.",
  },
  robots: { index: false, follow: false },
};

export default function SupportPreviewV7Page() {
  return <SupportPreviewV7Client />;
}
