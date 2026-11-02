import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaId, schemaRef } from "@/src/helpers/structuredData";

import { CheckCardSection } from "./CheckCardSection";
import { ClosingCta } from "./ClosingCta";
import { GateSection } from "./GateSection";
import { HeroSection } from "./HeroSection";
import { ImpactSection } from "./ImpactSection";
import { PipelineSection } from "./PipelineSection";
import { TimelineSection } from "./TimelineSection";

const PAGE = {
  title: "GraphQL Schema Checks for Safer Releases",
  description:
    "Run GraphQL schema checks against published client operations. See which versions a proposed change could break before you merge or deploy.",
  path: "/platform/release-safety",
  keywords: [
    "GraphQL schema checks",
    "breaking change detection",
    "schema registry",
    "client registry",
    "GraphQL schema validation CI",
    "safe schema evolution",
    "Nitro schema checks",
    "published operation impact",
    "schema linting",
    "GraphQL governance",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

export default function ReleaseSafetyPage() {
  return (
    <div className="flex flex-col gap-24 py-6 sm:gap-28">
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Platform", path: "/platform" },
          { name: "Release Safety" },
        ]}
        about={schemaRef(schemaId("/products/nitro", "product"))}
      />
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
