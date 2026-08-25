import { ClosingCta } from "@/src/components/support/ClosingCta";
import { ComparisonMatrix } from "@/src/components/support/ComparisonMatrix";
import { EnterpriseBand } from "@/src/components/support/EnterpriseBand";
import { PlanGrid, SUPPORT_PLANS } from "@/src/components/support/PlanGrid";
import {
  SupportFaq,
  SUPPORT_FAQ_ITEMS,
} from "@/src/components/support/SupportFaq";
import { SupportHero } from "@/src/components/support/SupportHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  createFaqNode,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const PAGE = {
  title: "GraphQL Support Plans for .NET Teams",
  description:
    "Get GraphQL support from the engineers behind Hot Chocolate, Fusion, and Nitro, with private channels, incident allowances, and defined response times.",
  path: "/services/support",
  keywords: [
    "GraphQL support",
    "Hot Chocolate support",
    "Nitro support",
    "ChilliCream support",
    "GraphQL support plans",
    "incident response",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SERVICE_ID = schemaId(PAGE.path, "service");
const OFFER_CATALOG_ID = schemaId(PAGE.path, "offers");
const OFFERS = SUPPORT_PLANS.map((plan) => ({
  "@type": "Offer",
  "@id": schemaId(PAGE.path, `offer-${plan.name.toLowerCase()}`),
  name: `${plan.name} GraphQL support`,
  description: plan.tagline,
  url: toAbsoluteUrl(plan.cta.href),
  seller: schemaRef(ORGANIZATION_ID),
  itemOffered: schemaRef(SERVICE_ID),
  availability: "https://schema.org/InStock",
  ...(plan.monthlyPrice !== undefined
    ? {
        price: plan.monthlyPrice,
        priceCurrency: "USD",
        priceSpecification: {
          "@type": "UnitPriceSpecification",
          price: plan.monthlyPrice,
          priceCurrency: "USD",
          unitText: "MONTH",
        },
      }
    : {}),
}));
const SERVICE = {
  "@type": "Service",
  "@id": SERVICE_ID,
  name: "GraphQL support plans",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  serviceType: "Production support for Hot Chocolate, Fusion, and Nitro",
  provider: schemaRef(ORGANIZATION_ID),
  hasOfferCatalog: schemaRef(OFFER_CATALOG_ID),
  offers: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const OFFER_CATALOG = {
  "@type": "OfferCatalog",
  "@id": OFFER_CATALOG_ID,
  name: "GraphQL support plans",
  url: toAbsoluteUrl(PAGE.path),
  itemListElement: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const FAQ = createFaqNode(PAGE.path, SUPPORT_FAQ_ITEMS);

export default function SupportPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Services", path: "/services" },
          { name: "Support" },
        ]}
        mainEntity={schemaRef(SERVICE_ID)}
        about={schemaRef(SERVICE_ID)}
        additionalNodes={[SERVICE, OFFER_CATALOG, ...OFFERS, FAQ]}
      />
      <SupportHero />
      <PlanGrid />
      <ComparisonMatrix />
      <SupportFaq />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}
