import { CardGrid } from "@/src/components/CardGrid";
import { ContentSection } from "@/src/components/ContentSection";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "Strawberry Shake",
  description:
    "Strawberry Shake is a strongly-typed GraphQL client for .NET with a reactive store, caching, subscriptions, and compile-time source generators.",
  path: "/products/strawberryshake",
});

const FEATURES = [
  {
    title: "Strongly-typed Client",
    description:
      "Generate strongly-typed .NET clients from your GraphQL schema and queries. No more runtime parsing surprises.",
  },
  {
    title: "Reactive Store",
    description:
      "Built-in reactive store with caching, optimistic updates, and offline support. Just wire it up.",
  },
  {
    title: "Subscriptions",
    description:
      "First-class GraphQL subscriptions over WebSockets or Server-Sent Events.",
  },
  {
    title: "Source Generators",
    description:
      "Roslyn source generators emit strongly-typed code at compile time — no IL weaving, no runtime cost.",
  },
];

export default function StrawberryShakePage() {
  return (
    <>
      <PageHero
        eyebrow="GraphQL Client for .NET"
        title="Strawberry Shake"
        teaser="A strongly-typed GraphQL client for .NET with reactive state, caching, and subscriptions baked in."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/strawberryshake">Get Started</SolidButton>
        <OutlineButton href="https://github.com/ChilliCream/graphql-platform">
          View on GitHub
        </OutlineButton>
      </div>

      <Section title="Built for .NET Teams">
        <CardGrid cols={2} breakpoint="sm" gap={6}>
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
        title="MIT Licensed"
        text="Free for any project, commercial or otherwise."
      />
    </>
  );
}
