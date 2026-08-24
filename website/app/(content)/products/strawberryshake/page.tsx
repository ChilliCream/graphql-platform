import { CardGrid } from "@/src/components/CardGrid";
import { ContentSection } from "@/src/components/ContentSection";
import { PageHero } from "@/src/components/PageHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Section } from "@/src/components/Section";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { Card } from "@/src/design-system/Card";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "Strawberry Shake: GraphQL Client for .NET",
  description:
    "Strawberry Shake is a type-safe GraphQL client for .NET that generates C# clients at build time and adds reactive caching and WebSocket subscriptions.",
  path: "/products/strawberryshake",
  keywords: [
    "GraphQL client for .NET",
    "type-safe GraphQL client",
    "C# GraphQL client",
    "Strawberry Shake GraphQL",
    "reactive GraphQL client",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Strawberry Shake",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  codeRepository: "https://github.com/ChilliCream/graphql-platform",
  programmingLanguage: {
    "@type": "ComputerLanguage",
    name: "C#",
  },
  runtimePlatform: ".NET",
  license: "https://opensource.org/license/mit",
  creator: schemaRef(ORGANIZATION_ID),
  publisher: schemaRef(ORGANIZATION_ID),
} as const;

const FEATURES = [
  {
    title: "Strongly-typed Client",
    description:
      "Write operations in .graphql files and build your project. Strawberry Shake generates typed C# results, inputs, variables, and client APIs.",
  },
  {
    title: "Normalized Reactive Store",
    description:
      "Normalize GraphQL results into an entity store that keeps watched operations and UI components in sync as data changes.",
  },
  {
    title: "Flexible Fetch Strategies",
    description:
      "Choose network-only, cache-first, or cache-and-network behavior for each operation and reuse cached entities across results.",
  },
  {
    title: "WebSocket Subscriptions",
    description:
      "Consume GraphQL subscription streams through the same generated client and update the reactive store as new results arrive.",
  },
];

export default function StrawberryShakePage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Strawberry Shake" },
        ]}
        mainEntity={schemaRef(SOFTWARE_ID)}
        about={schemaRef(SOFTWARE_ID)}
        additionalNodes={[SOFTWARE]}
      />
      <PageHero
        eyebrow="GraphQL Client for .NET"
        title="Strawberry Shake"
        teaser="A strongly-typed GraphQL client for .NET with reactive state, caching, and subscriptions baked in."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/strawberryshake/get-started">
          Build Your First .NET Client
        </SolidButton>
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
        text="Use, modify, and distribute Strawberry Shake in commercial or private projects under the terms of the MIT license. It works with spec-compliant GraphQL servers, including Hot Chocolate."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <OutlineButton href="/products/hotchocolate">
          Build a GraphQL Server for .NET
        </OutlineButton>
        <OutlineButton href="/docs/strawberryshake/caching">
          Explore Reactive Caching
        </OutlineButton>
      </div>
    </>
  );
}
