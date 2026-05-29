import Link from "next/link";

import { ContentSection } from "@/src/components/ContentSection";
import { PageHero, Section } from "@/src/components/SectionTitle";

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
        <Link
          href="/docs/strawberryshake"
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

      <Section title="Built for .NET Teams">
        <div className="grid gap-6 sm:grid-cols-2">
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
        title="MIT Licensed"
        text="Free for any project, commercial or otherwise."
      />
    </>
  );
}
