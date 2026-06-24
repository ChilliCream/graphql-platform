import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Mocha messaging .NET: Source-Generated Mediator and Bus",
  description:
    "Mocha messaging .NET is the open-source source-generated mediator and message bus: validated sagas, transactional outbox, idempotent inbox, every hop a span.",
  keywords: [
    "Mocha messaging .NET",
    "Mocha",
    ".NET messaging",
    "in-process mediator",
    "message bus",
    "CQRS",
    "Roslyn source generator",
    "RabbitMQ",
    "Postgres outbox",
    "transactional outbox",
    "idempotent inbox",
    "saga orchestration",
    "OpenTelemetry",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Mocha messaging .NET: Mediator and Bus",
    description:
      "One source-generated framework for in-process CQRS and cross-service messaging on .NET. RabbitMQ, Postgres, in-process, outbox and inbox, sagas, traces.",
    type: "website",
  },
};

export default function MochaPreviewV7() {
  return <ClientPage />;
}
