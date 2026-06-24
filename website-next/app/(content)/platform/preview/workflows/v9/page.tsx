import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Workflows Quarterly: Mocha Mediator & Bus for .NET",
  description:
    "An editorial spread on Mocha, the source-generated mediator and message bus for .NET, with validated sagas, pluggable transports, and outbox reliability.",
  keywords: [
    "mocha",
    "dotnet message bus",
    "in-process mediator",
    "CQRS",
    "sagas",
    "transactional outbox",
    "idempotent inbox",
    "exactly-once processing",
    "RabbitMQ",
    "Kafka",
    "Postgres transport",
    "Azure Service Bus",
    "event-driven architecture",
    "OpenTelemetry tracing",
  ],
  openGraph: {
    title: "Mocha Workflows Quarterly: mediator + message bus for .NET",
    description:
      "Hand the slow work to a message and let it keep moving after the response goes out. A magazine spread on source-generated CQRS and cross-service messaging.",
  },
  robots: { index: false, follow: false },
};

export default function WorkflowsPreviewV9() {
  return <ClientPage />;
}
