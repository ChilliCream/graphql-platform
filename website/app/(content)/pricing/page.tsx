import { ButtonRow } from "@/src/components/ButtonRow";
import { MarketingHero } from "@/src/components/MarketingHero";
import { PageStructuredData } from "@/src/components/PageStructuredData";
import { ClosingCta } from "@/src/components/pricing/ClosingCta";
import { CompareTable } from "@/src/components/pricing/CompareTable";
import { PlanSelector } from "@/src/components/pricing/PlanSelector";
import {
  PricingFaq,
  PRICING_FAQ_ITEMS,
} from "@/src/components/pricing/PricingFaq";
import { RegulatedBand } from "@/src/components/pricing/RegulatedBand";
import { OutlineButton, SolidButton } from "@/src/design-system/Button";
import {
  createNitroOfferCatalogNode,
  createNitroOfferNodes,
  createNitroProductNode,
  NITRO_OFFER_CATALOG_ID,
  NITRO_PRODUCT_ID,
} from "@/src/helpers/nitroStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { createFaqNode, schemaRef } from "@/src/helpers/structuredData";

const PAGE = {
  title: "Nitro Pricing and Deployment Plans",
  description:
    "Start Nitro free on shared cloud, pay as you go from $20 per month, choose Dedicated from $400, BYOC, or self-hosted. Compare usage, retention, and support.",
  path: "/pricing",
  keywords: [
    "Nitro pricing",
    "GraphQL platform pricing",
    "Nitro plans",
    "dedicated GraphQL platform",
    "self-hosted GraphQL platform",
    "BYOC GraphQL",
    "schema registry pricing",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const OFFERS = createNitroOfferNodes();
const PRODUCT = createNitroProductNode(PAGE.description, OFFERS);
const OFFER_CATALOG = createNitroOfferCatalogNode(OFFERS);
const FAQ = createFaqNode(PAGE.path, PRICING_FAQ_ITEMS);

export default function PricingPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Pricing" }]}
        mainEntity={schemaRef(NITRO_OFFER_CATALOG_ID)}
        about={schemaRef(NITRO_PRODUCT_ID)}
        additionalNodes={[PRODUCT, OFFER_CATALOG, ...OFFERS, FAQ]}
      />
      <Hero />
      <PlanSelector />
      <CompareTable />
      <PricingFaq />
      <RegulatedBand />
      <ClosingCta />
    </>
  );
}

function Hero() {
  return (
    <MarketingHero
      eyebrow="Nitro pricing"
      title="Pricing that scales with your platform."
      lead="Start on the shared cloud, pay for more usage as your API grows, choose a dedicated or BYOC deployment for greater isolation and control, or run Nitro on your own infrastructure."
      actions={
        <ButtonRow align="center">
          <SolidButton href="https://nitro.chillicream.com">
            Start Nitro for Free
          </SolidButton>
          <OutlineButton href="/services/support/contact?subject=Sales&context=Private%20Nitro%20Deployment">
            Discuss a private deployment
          </OutlineButton>
        </ButtonRow>
      }
    />
  );
}
