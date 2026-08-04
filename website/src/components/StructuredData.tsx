import { SITE_NAME, SITE_TITLE } from "@/src/helpers/site";
import { SITE_URL } from "@/src/helpers/siteUrl";

/**
 * Stable JSON-LD `@graph` describing the organization and the site itself.
 * Emitted once in the root layout so it applies site-wide; `@id` anchors let
 * per-page structured data reference these nodes later if needed.
 */
const STRUCTURED_DATA = {
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "Organization",
      "@id": `${SITE_URL}/#organization`,
      name: SITE_NAME,
      url: SITE_URL,
      logo: `${SITE_URL}/icon.png`,
      sameAs: [
        "https://github.com/ChilliCream/graphql-platform",
        "https://x.com/Chilli_Cream",
        "https://www.linkedin.com/company/chillicream",
        "https://www.youtube.com/c/ChilliCream",
      ],
    },
    {
      "@type": "WebSite",
      "@id": `${SITE_URL}/#website`,
      name: SITE_TITLE,
      url: SITE_URL,
      publisher: { "@id": `${SITE_URL}/#organization` },
    },
  ],
};

export function StructuredData() {
  return (
    <script
      type="application/ld+json"
      dangerouslySetInnerHTML={{ __html: JSON.stringify(STRUCTURED_DATA) }}
    />
  );
}
