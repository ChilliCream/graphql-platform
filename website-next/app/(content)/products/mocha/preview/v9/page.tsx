import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Mocha messaging .NET: Pinball-table flow for messages",
  description:
    "Mocha messaging .NET routes commands like a pinball table: source-generated mediator, bus, transactional outbox, validated sagas, exactly-once inbox, traces.",
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
    title: "Mocha messaging .NET: Pinball-table flow for messages",
    description:
      "One source-generated framework for in-process CQRS and cross-service messaging on .NET. Mediator, bus, outbox, inbox, sagas, traces, as a pinball table.",
    type: "website",
  },
};

export default function MochaPreviewV9() {
  return <ClientPage />;
}
