import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Mocha messaging .NET: Signal on the Wire",
  description:
    "Mocha messaging .NET is the open-source source-generated mediator and bus: validated sagas, transactional outbox, idempotent inbox, every hop visualized as signal.",
  keywords: [
    "Mocha messaging .NET",
    "Mocha",
    ".NET messaging",
    "in-process mediator",
    "message bus",
    "CQRS",
    "Roslyn source generator",
    "RabbitMQ",
    "Azure Service Bus",
    "Postgres outbox",
    "transactional outbox",
    "idempotent inbox",
    "saga orchestration",
    "OpenTelemetry",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Mocha messaging .NET: Signal on the Wire",
    description:
      "One source-generated framework for in-process CQRS and cross-service messaging on .NET. RabbitMQ, Azure Service Bus, Postgres, outbox and inbox, validated sagas.",
    type: "website",
  },
};

export default function MochaPreviewV8() {
  return <ClientPage />;
}
