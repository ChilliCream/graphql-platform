import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Trace Waterfall in Motion: GraphQL Observability for .NET",
  description:
    "GraphQL observability for .NET, OTel-native. Watch a live distributed-trace waterfall draw across GraphQL, REST, gRPC, and jobs with p95, p99, impact.",
  keywords: [
    "GraphQL observability",
    "OpenTelemetry .NET",
    "distributed tracing",
    "trace waterfall",
    "Nitro telemetry",
    "p95 p99 latency",
    "impact score",
    "Hot Chocolate observability",
  ],
  openGraph: {
    title: "Trace Waterfall in Motion",
    description:
      "A live distributed-trace waterfall draws itself across GraphQL, REST, gRPC, and background jobs. The slow gRPC hop paints coral, the rest stay teal.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
