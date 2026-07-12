import { CardGrid } from "@/src/components/CardGrid";
import { ContentSection } from "@/src/components/ContentSection";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Hot Chocolate",
  description:
    "Hot Chocolate is the GraphQL server for .NET: build type-safe APIs with DataLoader batching, subscriptions, OpenTelemetry, and federation via Fusion.",
  path: "/products/hotchocolate",
});

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
        <SolidButton href="/docs/hotchocolate">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>

      <Section title="Built for Production">
        <CardGrid cols={3} step="progressive" gap={6}>
          {FEATURES.map((feature) => (
            <Card key={feature.title} variant="tile">
              <h3 className="text-cc-ink text-lg font-semibold">
                {feature.title}
              </h3>
              <p className="text-cc-ink-dim mt-2 text-sm">
                {feature.description}
              </p>
            </Card>
          ))}
        </CardGrid>
      </Section>

      <ContentSection
        title="MIT Licensed, Free to Use"
        text="Hot Chocolate is open source under the MIT license. Use it in any project — commercial or otherwise — with no strings attached."
      />
    </>
  );
}
