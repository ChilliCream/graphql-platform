import { TIERS } from "@/src/components/pricing/pricingData";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  type JsonLdNode,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

export const NITRO_PRODUCT_ID = schemaId("/products/nitro", "product");
export const NITRO_OFFER_CATALOG_ID = schemaId("/pricing", "offers");

export function createNitroOfferNodes(): readonly JsonLdNode[] {
  return TIERS.map((tier) => {
    const exactPrice = tier.monthlyPrice;
    const minimumPrice = tier.minimumMonthlyPrice;

    return {
      "@type": "Offer",
      "@id": schemaId("/pricing", `offer-${tier.id}`),
      name: `Nitro ${tier.name}`,
      description: tier.tagline,
      url: toAbsoluteUrl(tier.ctaHref),
      availability: "https://schema.org/InStock",
      seller: schemaRef(ORGANIZATION_ID),
      itemOffered: schemaRef(NITRO_PRODUCT_ID),
      ...(exactPrice !== undefined
        ? {
            price: exactPrice,
            priceCurrency: "USD",
            priceSpecification: {
              "@type": "UnitPriceSpecification",
              price: exactPrice,
              priceCurrency: "USD",
              unitText: "MONTH",
            },
          }
        : {}),
      ...(minimumPrice !== undefined
        ? {
            priceSpecification: {
              "@type": "UnitPriceSpecification",
              minPrice: minimumPrice,
              priceCurrency: "USD",
              unitText: "MONTH",
            },
          }
        : {}),
    };
  });
}

export function createNitroProductNode(
  description: string,
  offers: readonly JsonLdNode[],
): JsonLdNode {
  return {
    "@type": "Product",
    "@id": NITRO_PRODUCT_ID,
    name: "Nitro",
    description,
    url: toAbsoluteUrl("/products/nitro"),
    image: toAbsoluteUrl("/images/nitro/nitro-app.png"),
    category: "GraphQL observability and API operations platform",
    brand: schemaRef(ORGANIZATION_ID),
    manufacturer: schemaRef(ORGANIZATION_ID),
    offers: offers.map((offer) => schemaRef(offer["@id"]!)),
  };
}

export function createNitroOfferCatalogNode(
  offers: readonly JsonLdNode[],
): JsonLdNode {
  return {
    "@type": "OfferCatalog",
    "@id": NITRO_OFFER_CATALOG_ID,
    name: "Nitro plans and deployment options",
    url: toAbsoluteUrl("/pricing"),
    itemListElement: offers.map((offer) => schemaRef(offer["@id"]!)),
  };
}
