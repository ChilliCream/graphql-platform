import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "tail -f Production: GraphQL Observability for .NET",
  description:
    "GraphQL observability for .NET, OpenTelemetry-native. Watch a checkout incident from a p99 spike down to the slow gRPC span, streamed in a live terminal.",
  keywords: [
    "GraphQL observability",
    "OpenTelemetry .NET",
    "distributed tracing",
    "Nitro telemetry",
    "p95 p99 latency",
    "operation monitoring",
    "Hot Chocolate observability",
    "trace waterfall",
  ],
  openGraph: {
    title: "tail -f Production",
    description:
      "GraphQL observability for .NET, staged as a live terminal. p99 spikes, distributed traces, and the slow span already highlighted.",
  },
  robots: { index: false, follow: false },
};

export default function ObservabilityV8Page() {
  return <ClientPage />;
}
