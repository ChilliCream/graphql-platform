import type { Metadata } from "next";

import { AnalyticsPreviewV7View } from "./view";

export const metadata: Metadata = {
  title: "Nitro Analytics: Animated GraphQL & .NET Observability",
  description:
    "GraphQL observability for .NET via Nitro: animated trace waterfalls, ranked impact, p95/p99, per-client share, and cross-service OpenTelemetry telemetry.",
  keywords: [
    "GraphQL observability for .NET",
    "Nitro analytics",
    "OpenTelemetry .NET",
    "distributed tracing",
    "p95 p99 latency",
    "impact score",
    "Hot Chocolate telemetry",
  ],
  openGraph: {
    title: "Nitro Analytics: Waterfall in Motion",
    description:
      "GraphQL observability for .NET, drawn live: animated trace waterfall, ranked impact, per-client share, cross-service OpenTelemetry, all on the same pipeline.",
  },
  robots: { index: false, follow: false },
};

export default function AnalyticsPreviewV7Page() {
  return <AnalyticsPreviewV7View />;
}
