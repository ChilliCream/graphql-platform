import type { Metadata } from "next";

import { PlatformLiveCircuitClient } from "./ClientPage";

/* -------------------------------------------------------------------------- */
/*  Metadata                                                                  */
/*  Next.js does not allow `export const metadata` from a Client Component    */
/*  module, so this server-component page exports the metadata and renders    */
/*  the client subtree where the motion hooks live.                           */
/* -------------------------------------------------------------------------- */

export const metadata: Metadata = {
  title: "The ChilliCream Platform: Live Circuit",
  description:
    "ChilliCream as one live GraphQL circuit. Watch a request pulse through eight platform surfaces and the Nitro control plane that keeps every API in sync.",
  keywords: [
    "ChilliCream platform",
    "GraphQL platform overview",
    "GraphQL observability",
    "GraphQL workflows",
    "GraphQL release safety",
    "GraphQL analytics",
    "Nitro control plane",
  ],
  openGraph: {
    title: "The ChilliCream Platform: Live Circuit",
    description:
      "ChilliCream as one live GraphQL circuit. Watch a request pulse through eight platform surfaces and the Nitro control plane that keeps every API in sync.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformLiveCircuitPage() {
  return <PlatformLiveCircuitClient />;
}
