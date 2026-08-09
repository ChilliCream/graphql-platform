import { SITE_NAME } from "@/src/helpers/site";
import { SITE_URL } from "@/src/helpers/siteUrl";
import {
  LOGO_ID,
  ORGANIZATION_ID,
  type JsonLdGraph,
  WEBSITE_ID,
} from "@/src/helpers/structuredData";

import { JsonLd } from "./JsonLd";

/**
 * Stable JSON-LD `@graph` describing the organization and the site itself.
 * Emitted once in the root layout so it applies site-wide; `@id` anchors let
 * per-page structured data reference these nodes later if needed.
 */
const STRUCTURED_DATA: JsonLdGraph = {
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "Organization",
      "@id": ORGANIZATION_ID,
      name: SITE_NAME,
      legalName: "ChilliCream, Inc.",
      description:
        "ChilliCream builds open-source GraphQL and messaging tools for .NET, plus the Nitro API operations platform.",
      url: SITE_URL,
      logo: { "@id": LOGO_ID },
      email: "contact@chillicream.com",
      contactPoint: {
        "@type": "ContactPoint",
        contactType: "customer support and sales",
        email: "contact@chillicream.com",
        url: `${SITE_URL}/services/support/contact`,
        availableLanguage: "English",
      },
      address: {
        "@type": "PostalAddress",
        streetAddress: "1207 Delaware Ave #3567",
        addressLocality: "Wilmington",
        addressRegion: "DE",
        postalCode: "19806",
        addressCountry: "US",
      },
      sameAs: [
        "https://github.com/ChilliCream/graphql-platform",
        "https://x.com/Chilli_Cream",
        "https://www.linkedin.com/company/chillicream",
        "https://www.youtube.com/c/ChilliCream",
      ],
    },
    {
      "@type": "ImageObject",
      "@id": LOGO_ID,
      url: `${SITE_URL}/icon.png`,
      contentUrl: `${SITE_URL}/icon.png`,
      width: 512,
      height: 512,
      caption: SITE_NAME,
    },
    {
      "@type": "WebSite",
      "@id": WEBSITE_ID,
      name: SITE_NAME,
      alternateName: "ChilliCream GraphQL Platform",
      url: SITE_URL,
      publisher: { "@id": ORGANIZATION_ID },
      inLanguage: "en",
    },
  ],
};

export function StructuredData() {
  return <JsonLd id="site-structured-data" data={STRUCTURED_DATA} />;
}
