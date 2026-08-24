import { pageMetadata } from "@/src/helpers/pageMetadata";
import { SITE_URL } from "@/src/helpers/siteUrl";

import { CheckCardSection } from "./CheckCardSection";
import { ClosingCta } from "./ClosingCta";
import { GateSection } from "./GateSection";
import { HeroSection } from "./HeroSection";
import { ImpactSection } from "./ImpactSection";
import { PipelineSection } from "./PipelineSection";
import { TimelineSection } from "./TimelineSection";

export const metadata = pageMetadata({
  title: "GraphQL Schema Checks and Release Safety",
  description:
    "Catch breaking GraphQL schema changes before they ship. Nitro's schema checks validate proposed schemas against the operations your clients use in each environment.",
  path: "/platform/release-safety",
  keywords: [
    "GraphQL schema release safety",
    "breaking change detection",
    "schema registry",
    "client registry",
    "schema validation CI",
    "safe schema evolution",
    "Nitro schema checks",
    "published operation impact",
    "schema linting",
    "GraphQL governance",
  ],
});

const BREADCRUMB_DATA = {
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  itemListElement: [
    {
      "@type": "ListItem",
      position: 1,
      name: "Home",
      item: `${SITE_URL}/`,
    },
    {
      "@type": "ListItem",
      position: 2,
      name: "Platform",
      item: `${SITE_URL}/platform`,
    },
    {
      "@type": "ListItem",
      position: 3,
      name: "Release Safety",
    },
  ],
};

export default function ReleaseSafetyPage() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <script type="application/ld+json" dangerouslySetInnerHTML={{ __html: JSON.stringify(BREADCRUMB_DATA) }} />
      <HeroSection />
      <CheckCardSection />
      <ImpactSection />
      <GateSection />
      <PipelineSection />
      <TimelineSection />
      <ClosingCta />
    </div>
  );
}
