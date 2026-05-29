import Link from "next/link";

import { ContentSection } from "@/src/components/ContentSection";
import { PageHero, Section } from "@/src/components/SectionTitle";

const FEATURES = [
  {
    title: "Compile-time Composition",
    description:
      "Fusion composes subgraph schemas at planning time, not runtime. The gateway stays fast and queries stay typed end-to-end.",
  },
  {
    title: "Code-first or Schema-first",
    description:
      "Author your GraphQL schema however your team prefers. Hot Chocolate supports both styles with full type safety.",
  },
  {
    title: "DataLoader Batching",
    description:
      "Green Donut batches loads at the federation layer so cross-service N+1 disappears automatically.",
  },
  {
    title: "Realtime Subscriptions",
    description:
      "Server-sent events and WebSocket subscriptions are first-class — no extra wiring required.",
  },
  {
    title: "OpenTelemetry Built In",
    description:
      "Traces, errors, and per-resolver latency wire into your existing OTel backend (Jaeger, Tempo, Datadog, Honeycomb).",
  },
  {
    title: "Federation-ready",
    description:
      "Compose with other Hot Chocolate services via Fusion or with Apollo subgraphs via the Federation spec.",
  },
];

export default function HotChocolatePage() {
  return (
    <>
      <PageHero
        eyebrow="GraphQL Server for .NET"
        title="Hot Chocolate"
        teaser="The fastest way to build production GraphQL APIs in .NET. Type-safe end to end, federation-ready, and battle-tested at scale."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <Link
          href="/docs/hotchocolate"
          className="inline-flex items-center rounded-full bg-[var(--cc-ink)] px-7 py-3 text-sm font-medium text-[#0c1322] no-underline transition-colors hover:bg-white"
        >
          Get Started
        </Link>
        <Link
          href="https://github.com/ChilliCream/graphql-platform"
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center rounded-full border border-[var(--cc-card-border)] px-7 py-3 text-sm font-medium text-[var(--cc-ink)] no-underline transition-colors hover:border-[var(--cc-card-border-hover)] hover:text-[var(--cc-ink)]"
        >
          View on GitHub
        </Link>
      </div>

      <Section title="Built for Production">
        <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
          {FEATURES.map((feature) => (
            <div
              key={feature.title}
              className="rounded-xl border border-[var(--cc-card-border)] bg-[var(--cc-card-bg)] backdrop-blur-sm p-6 "
            >
              <h3 className="text-lg font-semibold text-[var(--cc-ink)]">
                {feature.title}
              </h3>
              <p className="mt-2 text-sm text-[var(--cc-ink-dim)]">
                {feature.description}
              </p>
            </div>
          ))}
        </div>
      </Section>

      <ContentSection
        title="MIT Licensed, Free to Use"
        text="Hot Chocolate is open source under the MIT license. Use it in any project — commercial or otherwise — with no strings attached."
      />
    </>
  );
}
