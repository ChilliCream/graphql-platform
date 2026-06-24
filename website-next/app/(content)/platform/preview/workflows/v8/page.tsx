import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Workflows: Mocha Message on the Rail for .NET",
  description:
    "Let work continue after the request. Mocha is a source-generated mediator and cross-service message bus for .NET, with validated sagas and outbox reliability.",
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
    title: "Mocha: a message on the rail for .NET",
    description:
      "Follow one message down a timeline of hops: dispatch, publish, outbox, inbox, saga, transport, trace. One source-generated framework for CQRS and cross-service messaging.",
  },
  robots: { index: false, follow: false },
};

export default function WorkflowsPreviewV8() {
  return <ClientPage />;
}
