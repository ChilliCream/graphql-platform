import { ButtonRow } from "@/src/components/ButtonRow";
import { CardGrid } from "@/src/components/CardGrid";
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

import {
  AnyServerSection,
  BothSpecificationsSection,
  ClientSafetySection,
  NitroCta,
  WhatIsFusionSection,
} from "./sections";

const GETTING_STARTED_HREF = "/docs/fusion/getting-started";

const CONTACT_HREF = "/services/support/contact?subject=Sales&context=Fusion";

const PAGE = {
  title: "Fusion: GraphQL Federation Gateway",
  description:
    "Fusion is the GraphQL Federation gateway that composes source schemas from any GraphQL server, OpenAPI, and gRPC into one composite schema and executes every query across them.",
  path: "/products/fusion",
  keywords: [
    "GraphQL Federation gateway",
    "GraphQL gateway",
    "Apollo Federation compatible gateway",
    "composite schema",
    "Fusion GraphQL",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Fusion",
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
    title: "Both Federation Protocols",
    description:
      "Subgraphs written to the GraphQL Federation specification and subgraphs written to Apollo Federation compose into the same composite schema, so you can move one subgraph at a time.",
  },
  {
    title: "OpenAPI and gRPC Sources",
    description:
      "A service that publishes an OpenAPI document or a gRPC definition joins the same composite schema, with its contract validated in the same composition step.",
  },
  {
    title: "Any Language, Any Server",
    description:
      "A GraphQL subgraph stays an ordinary GraphQL server that declares its keys and lookups in its own schema. There is no distributed-runtime package to install alongside it.",
  },
  {
    title: "Composition in Your Pipeline",
    description:
      "Composition validates the source schemas against one another before deployment, so type conflicts, missing fields, and incompatible enums fail the build instead of the gateway.",
  },
  {
    title: "Hot-Swapped Configuration",
    description:
      "Publish a composed Fusion configuration from your pipeline to Nitro. The gateway subscribes to the latest archive and swaps it in without a restart.",
  },
  {
    title: "Federated Subscriptions",
    description:
      "Stream results that span several subgraphs with broker-backed federated event streams, or subscribe to a subgraph over Server-Sent Events.",
  },
];

export default function FusionPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Fusion" }]}
        mainEntity={schemaRef(SOFTWARE_ID)}
        about={schemaRef(SOFTWARE_ID)}
        additionalNodes={[SOFTWARE]}
      />
      <PageHero
        eyebrow="GraphQL Federation Gateway"
        title="Fusion"
        teaser="The gateway that composes your teams' source schemas into one composite schema and executes every query across the subgraphs. Fusion is the only gateway that supports both the GraphQL Federation specification and Apollo Federation, and it composes OpenAPI and gRPC sources too."
      />
      <ButtonRow>
        <SolidButton href={GETTING_STARTED_HREF}>Get Started</SolidButton>
        <OutlineButton href={CONTACT_HREF}>Contact an Expert</OutlineButton>
      </ButtonRow>

      <WhatIsFusionSection />

      <Section title="Built for Distributed Graphs">
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

      <BothSpecificationsSection />
      <AnyServerSection />
      <ClientSafetySection />
      <NitroCta />

      <ButtonRow>
        <SolidButton href={GETTING_STARTED_HREF}>Start with Fusion</SolidButton>
        <OutlineButton href={CONTACT_HREF}>Contact an Expert</OutlineButton>
      </ButtonRow>
    </>
  );
}
