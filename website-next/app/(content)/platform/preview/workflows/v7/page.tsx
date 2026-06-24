import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  robots: { index: false, follow: false },
  title: "Workflows: Mocha Mediator & Message Bus for .NET",
  description:
    "Follow one message through the Mocha topology. Source-generated event-driven workflows in .NET with mediator, bus, validated sagas, outbox plus inbox.",
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
    title: "Mocha Workflows: mediator + message bus for .NET",
    description:
      "Follow one message through the topology. Source-generated event-driven workflows in .NET with validated sagas and outbox plus inbox reliability.",
  },
};

export default function Page() {
  return <ClientPage />;
}
