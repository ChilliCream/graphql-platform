import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Analytics: One Column of GraphQL .NET Signal",
  description:
    "Nitro analytics is the OpenTelemetry-native dashboard for .NET GraphQL APIs: ranked impact, distributed traces, per-client usage, all in a single signal column.",
  keywords: [
    "GraphQL analytics",
    "Nitro analytics",
    "OpenTelemetry .NET",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "client usage",
    "Hot Chocolate telemetry",
    "cross-service monitoring",
  ],
  openGraph: {
    title: "Nitro Analytics: One Column of Signal",
    description:
      "One narrow column of telemetry signal for your .NET GraphQL API: ranked impact, distributed traces, per-client usage, all on OpenTelemetry.",
  },
  robots: { index: false, follow: false },
};

export default function AnalyticsPreviewV9Page() {
  return <ClientPage />;
}
