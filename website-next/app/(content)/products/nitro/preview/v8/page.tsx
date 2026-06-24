import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// v8 (Blueprint Cockpit): the Nitro product page rendered as a top-down
// architectural floor plan. Hairline cc-card-border lines form labeled "rooms",
// each holding one Nitro screen. The actual layout, motion, and hooks live in
// ClientPage so this file can stay a Server Component and emit `metadata`
// (Next App Router does not honor `metadata` from "use client" modules).

export const metadata: Metadata = {
  title: "Nitro: Blueprint of a GraphQL Control Plane",
  description:
    "Walk the Nitro GraphQL control plane room by room: observe, trace, diagnose, and evolve your GraphQL and .NET APIs on a single architectural floor plan.",
  keywords: [
    "Nitro",
    "GraphQL control plane",
    "GraphQL IDE",
    "OpenTelemetry",
    "distributed tracing",
    "schema registry",
    "Fusion gateway",
    "API observability",
    "ChilliCream",
    ".NET observability",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: Blueprint of a GraphQL Control Plane",
    description:
      "An architectural floor plan of Nitro: observe, trace, diagnose, evolve, and compose your GraphQL and .NET APIs in one cockpit.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
