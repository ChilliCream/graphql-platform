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
  title: "GraphQL Services for .NET Teams",
  description:
    "Work with the engineers behind Hot Chocolate, Fusion, and Nitro through GraphQL consulting, production support plans, or training for your .NET team.",
  path: "/services",
  keywords: [
    "GraphQL services",
    "GraphQL consulting services",
    "GraphQL support",
    "GraphQL training",
    ".NET GraphQL experts",
    "ChilliCream services",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SERVICE_SECTIONS = [
  {
    href: "/services/advisory",
    title: "GraphQL advisory",
    description:
      "Review an architecture, resolve a hard technical decision, or bring in the engineers who build the stack for a scoped implementation.",
  },
  {
    href: "/services/support",
    title: "GraphQL support",
    description:
      "Add a private support channel, defined incident allowances, and response times for the systems your team runs in production.",
  },
  {
    href: "/services/training",
    title: "GraphQL training",
    description:
      "Build shared GraphQL, Hot Chocolate, and Fusion skills through a curriculum shaped around your team's experience and codebase.",
  },
];

const ITEM_LIST = createItemListNode(
  PAGE.path,
  "ChilliCream GraphQL services",
  SERVICE_SECTIONS.map((section) => ({
    name: section.title,
    url: section.href,
    description: section.description,
    itemType: "Service",
  })),
);

export default function ServicesPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Services" }]}
        mainEntity={schemaRef(schemaId(PAGE.path, "item-list"))}
        additionalNodes={[ITEM_LIST]}
      />
      <PageHero
        eyebrow="ChilliCream services"
        title="GraphQL services for your .NET team."
        teaser="Choose a focused consulting engagement, ongoing production support, or team training from the engineers behind Hot Chocolate, Fusion, and Nitro."
      >
        <div className="mt-8">
          <ButtonRow align="center">
            <SolidButton href="/services/support/contact?subject=Sales&context=GraphQL%20Services">
              Discuss your project
            </SolidButton>
            <OutlineButton href="/help">Find the right help</OutlineButton>
          </ButtonRow>
        </div>
      </PageHero>
      <Section title="Choose the right way to work with us">
        <CardGrid cols={3}>
          {SERVICE_SECTIONS.map((section) => (
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
