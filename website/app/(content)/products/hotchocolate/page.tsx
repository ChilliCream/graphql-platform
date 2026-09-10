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
  title: "Hot Chocolate: GraphQL Server for .NET",
  description:
    "Hot Chocolate is the GraphQL server for .NET: build type-safe APIs with C# schema authoring, DataLoader, subscriptions, security, OpenTelemetry, and Fusion.",
  path: "/products/hotchocolate",
  keywords: [
    "GraphQL server for .NET",
    "ASP.NET Core GraphQL",
    "C# GraphQL API",
    "Hot Chocolate GraphQL",
    "GraphQL federation .NET",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Hot Chocolate",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  codeRepository: "https://github.com/ChilliCream/graphql-platform",
  programmingLanguage: {
    "@type": "ComputerLanguage",
    name: "C#",
  },
  runtimePlatform: ".NET and ASP.NET Core",
  license: "https://opensource.org/license/mit",
  creator: schemaRef(ORGANIZATION_ID),
  publisher: schemaRef(ORGANIZATION_ID),
} as const;

const FEATURES = [
  {
    title: "C# Schema Authoring",
    description:
      "Define the schema from your C# implementation or use fluent descriptors when you need precise control. Mix both approaches in the same app.",
  },
  {
    title: "DataLoader Batching",
    description:
      "Batch and cache related data fetches to reduce backend requests and address N+1 query patterns.",
  },
  {
    title: "Realtime Subscriptions",
    description:
      "Serve GraphQL over HTTP and deliver real-time results over WebSockets or Server-Sent Events from ASP.NET Core.",
  },
  {
    title: "OpenTelemetry Built In",
    description:
      "Emit GraphQL request, resolver, and DataLoader spans through the built-in OpenTelemetry integration.",
  },
  {
    title: "Cost Analysis and Trusted Documents",
    description:
      "Set operation cost budgets for open APIs or limit first-party apps to pre-registered documents.",
  },
  {
    title: "Federation-ready",
    description:
      "Start with one Hot Chocolate server, then compose services behind Fusion when teams need independent ownership and deployment.",
  },
];

export default function HotChocolatePage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Hot Chocolate" }]}
        mainEntity={schemaRef(SOFTWARE_ID)}
        about={schemaRef(SOFTWARE_ID)}
        additionalNodes={[SOFTWARE]}
      />
      <PageHero
        eyebrow="GraphQL Server for .NET"
        title="Hot Chocolate"
        teaser="The fastest way to build production GraphQL APIs in .NET. Type-safe end to end, federation-ready, and battle-tested at scale."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <SolidButton href="/docs/hotchocolate/get-started-with-graphql-in-net-core">
          Build Your First GraphQL API
        </SolidButton>
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
        text="Use, modify, and distribute Hot Chocolate in commercial or private projects under the terms of the MIT license. The source is available in the ChilliCream GraphQL Platform repository."
      />
      <div className="flex flex-wrap justify-center gap-4">
        <OutlineButton href="/products/strawberryshake">
          Add a Type-Safe .NET Client
        </OutlineButton>
        <OutlineButton href="/docs/fusion">Scale Out with Fusion</OutlineButton>
      </div>
    </>
  );
}
