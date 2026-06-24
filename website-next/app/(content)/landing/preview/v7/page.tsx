import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "ChilliCream: end-to-end GraphQL platform for .NET",
  description:
    "Platform Pulse: an animated request travels the GraphQL platform for .NET, from Strawberry Shake to Fusion to Hot Chocolate to Nitro telemetry, end to end.",
  keywords: [
    "ChilliCream",
    "GraphQL platform for .NET",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    "GraphQL observability",
    "schema registry",
    ".NET GraphQL",
  ],
  openGraph: {
    title: "ChilliCream: end-to-end GraphQL platform for .NET",
    description:
      "An animated Platform Pulse traces a request through Strawberry Shake, Fusion, Hot Chocolate, and Nitro. The end-to-end GraphQL platform for .NET in one loop.",
  },
  robots: { index: false, follow: false },
};

export default function LandingPreviewV7Page() {
  return <ClientPage />;
}
