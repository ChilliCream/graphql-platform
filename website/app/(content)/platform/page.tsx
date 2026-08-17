import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageHero } from "@/src/components/PageHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Section } from "@/src/components/Section";
import { ButtonRow } from "@/src/components/ButtonRow";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  createItemListNode,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "GraphQL API Lifecycle Tools for .NET",
  description:
    "Connect GraphQL federation, API analytics, schema checks, and agentic development with ChilliCream's open-source .NET tools and Nitro control plane.",
  path: "/platform",
  keywords: [
    "GraphQL API lifecycle tools",
    "GraphQL federation tools",
    "GraphQL API analytics",
    "GraphQL schema checks",
    "agentic development",
    "AI coding agents .NET",
    "ChilliCream API platform",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const PLATFORM_SECTIONS = [
  {
    href: "/platform/graphql-federation",
    title: "Federation with Fusion",
    description:
      "Compose independently owned GraphQL services into one gateway artifact before runtime.",
  },
  {
    href: "/platform/analytics",
    title: "Analytics",
    description: "Instant Insights. Enhanced Performance.",
  },
  {
    href: "/platform/release-safety",
    title: "Release Safety",
    description: "Catch Breaking Changes Before They Ship.",
  },
  {
    href: "/platform/ecosystem",
    title: "Ecosystem",
    description: "An Ecosystem You Trust and Love.",
  },
  {
    href: "/platform/agentic-coding",
    title: "Agentic Development",
    description: "Consistently Good Code, from Any Agent.",
  },
  {
    href: "/products/nitro",
    title: "Nitro Control Plane",
    description:
      "Bring operation analytics, traces, schema history, client safety, and delivery checks into one application.",
  },
];

const ITEM_LIST = createItemListNode(
  PAGE.path,
  "GraphQL API lifecycle tools",
  PLATFORM_SECTIONS.map((section) => ({
    name: section.title,
    url: section.href,
    description: section.description,
    itemType: "WebPage",
  })),
);

export default function PlatformPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Platform" }]}
        mainEntity={schemaRef(schemaId(PAGE.path, "item-list"))}
        additionalNodes={[ITEM_LIST]}
      />
      <PageHero
        title="The Platform"
        teaser="One platform for every API across your organization, from authoring and composition to operations and telemetry."
      >
        <div className="mt-8">
          <ButtonRow align="center">
            <SolidButton href="/products/nitro">Explore Nitro</SolidButton>
            <OutlineButton href="/docs/fusion">Read Fusion Docs</OutlineButton>
          </ButtonRow>
        </div>
      </PageHero>
      <Section title="Explore the Platform">
        <CardGrid cols={3}>
          {PLATFORM_SECTIONS.map((section) => (
            <LinkCard
              key={section.href}
              variant="trailing"
              href={section.href}
              title={section.title}
              description={section.description}
            />
          ))}
        </CardGrid>
      </Section>
    </>
  );
}
