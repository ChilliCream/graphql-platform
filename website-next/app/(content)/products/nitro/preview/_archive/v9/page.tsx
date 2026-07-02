import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// v9 (Manifest / Spec Sheet): the Nitro product page rendered as a
// machine-readable manifest. Each capability is a mono-typed spec record
// (type / since / depends-on / surface) anchored to a left hairline rail,
// closing with a fenced "runtime/dependencies.yaml" code block and a
// "$ run" CTA row. All hooks and motion logic live in ClientPage so this
// file stays a Server Component and can emit `metadata` (App Router does
// not honor metadata exports from a "use client" module).

export const metadata: Metadata = {
  title: "Nitro: Manifest of a GraphQL Control Plane",
  description:
    "The Nitro manifest, a spec sheet for the GraphQL control plane: schema registry, distributed tracing, diagnose, Fusion composition, telemetry, and IDE for .NET.",
  keywords: [
    "Nitro",
    "GraphQL control plane",
    "GraphQL IDE",
    "OpenTelemetry",
    "distributed tracing",
    "schema registry",
    "Fusion composition",
    "API observability",
    "ChilliCream",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Nitro: Manifest of a GraphQL Control Plane",
    description:
      "A spec sheet for Nitro: schema registry, tracing, diagnose, Fusion composition, telemetry, and IDE for GraphQL and .NET, declared field by field.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
