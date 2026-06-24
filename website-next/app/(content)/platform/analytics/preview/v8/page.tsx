import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Analytics: Switchback GraphQL .NET Observability",
  description:
    "Follow the signal switchback by switchback: GraphQL observability for .NET with ranked impact, distributed traces, per-client share, and cross-service OpenTelemetry.",
  keywords: [
    "GraphQL observability for .NET",
    "Nitro analytics",
    "OpenTelemetry .NET",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "per-client usage",
    "Hot Chocolate telemetry",
    "ChilliCream Nitro",
  ],
  openGraph: {
    title: "Nitro Analytics: Switchback Telemetry",
    description:
      "A zigzag path from impact ranking to trace, client, service, and proof. GraphQL observability for .NET, on open OpenTelemetry, end to end.",
  },
  robots: { index: false, follow: false },
};

export default function AnalyticsPreviewV8Page() {
  return <ClientPage />;
}
