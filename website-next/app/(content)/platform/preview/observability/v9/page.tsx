import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Production View: See What the GraphQL API Is Doing",
  description:
    "Nitro is OpenTelemetry-native GraphQL observability for .NET. A calm signal field of p95, p99, error rate, and traces across GraphQL, REST, gRPC, and jobs.",
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
    title: "See What the GraphQL API Is Doing",
    description:
      "A calm constellation of signals: operations, services, clients, traces. One incident, every lens, stitched by a shared trace id across GraphQL, REST, gRPC, and jobs.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
