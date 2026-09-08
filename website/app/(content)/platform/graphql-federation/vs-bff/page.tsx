import { PageStructuredData } from "@/src/components/PageStructuredData";

import { buildComparisonPage } from "../comparison/comparisonMetadata";
import { VsBffComparison } from "./content";

const {
  comparison,
  metadata: pageMetadata,
  structuredData,
} = buildComparisonPage("vs-bff");

export const metadata = pageMetadata;

export default function GraphQLFederationVsBffPage() {
  return (
    <>
      <PageStructuredData {...structuredData} />
      <VsBffComparison title={comparison.title} />
    </>
  );
}
