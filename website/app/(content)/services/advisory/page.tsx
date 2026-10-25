import {
  AdvisoryFaq,
  ADVISORY_FAQ_ITEMS,
} from "@/src/components/advisory/AdvisoryFaq";
import { AdvisoryHero } from "@/src/components/advisory/AdvisoryHero";
import { ContactBand } from "@/src/components/advisory/ContactBand";
import { EngagementStrip } from "@/src/components/advisory/EngagementStrip";
import { TeamSection } from "@/src/components/advisory/TeamSection";
import { ADVISORY_TIERS, TierGrid } from "@/src/components/advisory/TierGrid";
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
  title: "GraphQL Consulting and Advisory",
  description:
    "Get GraphQL consulting or a scoped implementation from the engineers behind Hot Chocolate, Fusion, and Nitro. Review your architecture or plan the build.",
  path: "/services/advisory",
  keywords: [
    "GraphQL consulting",
    "GraphQL advisory services",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    ".NET GraphQL consulting",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SERVICE_ID = schemaId(PAGE.path, "service");
const OFFER_CATALOG_ID = schemaId(PAGE.path, "offers");
const OFFERS = ADVISORY_TIERS.map((tier) => ({
  "@type": "Offer",
  "@id": schemaId(PAGE.path, `offer-${tier.id}`),
  name: tier.name,
  description: tier.tagline,
  url: toAbsoluteUrl(PAGE.path),
  seller: schemaRef(ORGANIZATION_ID),
  itemOffered: schemaRef(SERVICE_ID),
}));
const SERVICE = {
  "@type": "Service",
  "@id": SERVICE_ID,
  name: "GraphQL consulting and advisory",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  serviceType: "GraphQL architecture, consulting, and implementation",
  provider: schemaRef(ORGANIZATION_ID),
  hasOfferCatalog: schemaRef(OFFER_CATALOG_ID),
  offers: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const OFFER_CATALOG = {
  "@type": "OfferCatalog",
  "@id": OFFER_CATALOG_ID,
  name: "GraphQL advisory engagement types",
  url: toAbsoluteUrl(PAGE.path),
  itemListElement: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const FAQ = createFaqNode(PAGE.path, ADVISORY_FAQ_ITEMS);

export default function AdvisoryPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Services", path: "/services" },
          { name: "Advisory" },
        ]}
        mainEntity={schemaRef(SERVICE_ID)}
        about={schemaRef(SERVICE_ID)}
        additionalNodes={[SERVICE, OFFER_CATALOG, ...OFFERS, FAQ]}
      />
      <AdvisoryHero />
      <TierGrid />
      <EngagementStrip />
      <TeamSection />
      <AdvisoryFaq />
      <ContactBand />
    </>
  );
}
