import { DeliveryFormatsSection } from "@/src/components/training/DeliveryFormatsSection";
import { FunBand } from "@/src/components/training/FunBand";
import { LevelsSection } from "@/src/components/training/LevelsSection";
import {
  OffersSection,
  TRAINING_OFFERS,
} from "@/src/components/training/OffersSection";
import { OutcomesSection } from "@/src/components/training/OutcomesSection";
import { TrainingClosingCta } from "@/src/components/training/TrainingClosingCta";
import {
  TrainingFaq,
  TRAINING_FAQ_ITEMS,
} from "@/src/components/training/TrainingFaq";
import { TrainingHero } from "@/src/components/training/TrainingHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  createFaqNode,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

const META_DESCRIPTION =
  "Book GraphQL training for your .NET team, with Hot Chocolate, Fusion, schema design, performance, and client topics shaped for beginner or advanced engineers.";

const PAGE = {
  title: "GraphQL Training for .NET Teams",
  description: META_DESCRIPTION,
  path: "/services/training",
  keywords: [
    "GraphQL training",
    "Hot Chocolate training",
    "GraphQL workshop",
    "corporate GraphQL training",
    "Fusion training",
    "ASP.NET Core GraphQL training",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SERVICE_ID = schemaId(PAGE.path, "service");
const OFFER_CATALOG_ID = schemaId(PAGE.path, "offers");
const OFFERS = TRAINING_OFFERS.map((offer, index) => ({
  "@type": "Offer",
  "@id": schemaId(PAGE.path, `offer-${index + 1}`),
  name: offer.kind,
  description: offer.description,
  url: toAbsoluteUrl(PAGE.path),
  seller: schemaRef(ORGANIZATION_ID),
  itemOffered: schemaRef(SERVICE_ID),
}));
const SERVICE = {
  "@type": "Service",
  "@id": SERVICE_ID,
  name: "GraphQL training for .NET teams",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  serviceType: "GraphQL team training and hands-on workshops",
  provider: schemaRef(ORGANIZATION_ID),
  hasOfferCatalog: schemaRef(OFFER_CATALOG_ID),
  offers: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const OFFER_CATALOG = {
  "@type": "OfferCatalog",
  "@id": OFFER_CATALOG_ID,
  name: "GraphQL training engagement types",
  url: toAbsoluteUrl(PAGE.path),
  itemListElement: OFFERS.map((offer) => schemaRef(offer["@id"])),
} as const;
const FAQ = createFaqNode(PAGE.path, TRAINING_FAQ_ITEMS);

export default function TrainingPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Services", path: "/services" },
          { name: "Training" },
        ]}
        mainEntity={schemaRef(SERVICE_ID)}
        about={schemaRef(SERVICE_ID)}
        additionalNodes={[SERVICE, OFFER_CATALOG, ...OFFERS, FAQ]}
      />
      <TrainingHero />
      <LevelsSection />
      <OffersSection />
      <OutcomesSection />
      <DeliveryFormatsSection />
      <FunBand />
      <TrainingFaq />
      <TrainingClosingCta />
    </>
  );
}
