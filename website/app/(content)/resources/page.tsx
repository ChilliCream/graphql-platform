import { CardGrid } from "@/src/components/CardGrid";
import { LinkCard } from "@/src/components/LinkCard";
import { PageHero } from "@/src/components/PageHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { Section } from "@/src/components/Section";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  createItemListNode,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "Company Resources",
  description:
    "Find ChilliCream contact details, GraphQL services, Nitro pricing, commercial license terms, company policies, and official merchandise in one place.",
  path: "/resources",
  keywords: [
    "ChilliCream resources",
    "ChilliCream contact",
    "ChilliCream policies",
    "ChilliCream license",
    "ChilliCream shop",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const COMPANY_LINKS = [
  {
    href: "/services/support/contact",
    title: "Contact ChilliCream",
    description: "Discuss Nitro, GraphQL services, training, or support.",
  },
  {
    href: "/services",
    title: "GraphQL services",
    description: "Compare advisory, support, and team training.",
  },
  {
    href: "/pricing",
    title: "Nitro pricing",
    description: "Compare shared-cloud, dedicated, and self-hosted plans.",
  },
  {
    href: "https://store.chillicream.com",
    title: "Shop",
    description: "ChilliCream merch and goodies.",
    external: true,
  },
  {
    href: "/legal/acceptable-use-policy",
    title: "Acceptable Use Policy",
    description: "Rules for using ChilliCream services.",
  },
  {
    href: "/legal/cookie-policy",
    title: "Cookie Policy",
    description: "How we use cookies.",
  },
  {
    href: "/legal/privacy-policy",
    title: "Privacy Policy",
    description: "How we handle your data.",
  },
  {
    href: "/legal/terms-of-service",
    title: "Terms of Service",
    description: "The agreement between you and us.",
  },
  {
    href: "/licensing/chillicream-license",
    title: "ChilliCream License",
    description: "Commercial license terms.",
  },
];

const ITEM_LIST = createItemListNode(
  PAGE.path,
  "ChilliCream company links and policies",
  COMPANY_LINKS.map((link) => ({
    name: link.title,
    url: link.href,
    description: link.description,
    itemType: "WebPage",
  })),
  { order: "https://schema.org/ItemListUnordered" },
);

export default function ResourcesPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="CollectionPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Resources" }]}
        mainEntity={schemaRef(schemaId(PAGE.path, "item-list"))}
        additionalNodes={[ITEM_LIST]}
      />
      <PageHero
        eyebrow="Resources"
        title="ChilliCream company resources."
        teaser="Contact the team, compare commercial options, review our policies and license terms, or visit the official ChilliCream shop."
      />
      <Section title="Company links and policies">
        <CardGrid cols={3} step="progressive">
          {COMPANY_LINKS.map((link) => (
            <LinkCard
              key={link.href}
              variant="plain"
              href={link.href}
              title={link.title}
              description={link.description}
              external={link.external}
            />
          ))}
        </CardGrid>
      </Section>
    </>
  );
}
