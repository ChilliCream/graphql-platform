import type { Metadata } from "next";

import { ContentSection } from "@/src/components/ContentSection";
import { PageHero } from "@/src/components/PageHero";
import { Section } from "@/src/components/Section";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";

export const metadata: Metadata = {
  title: "Strawberry Shake",
  description:
    "Strawberry Shake is a strongly-typed GraphQL client for .NET with a reactive store, caching, subscriptions, and compile-time source generators.",
};

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
        <div className="grid gap-6 sm:grid-cols-2">
          {FEATURES.map((feature) => (
            <div
              key={feature.title}
              className="rounded-xl border border-cc-card-border bg-cc-card-bg backdrop-blur-sm p-6 "
            >
              <h3 className="text-lg font-semibold text-cc-ink">
                {feature.title}
              </h3>
              <p className="mt-2 text-sm text-cc-ink-dim">
                {feature.description}
              </p>
            </div>
          ))}
        </div>
      </Section>

      <ContentSection
        title="MIT Licensed"
        text="Free for any project, commercial or otherwise."
      />
    </>
  );
}
