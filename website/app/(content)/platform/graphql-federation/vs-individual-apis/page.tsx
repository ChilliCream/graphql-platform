import { PageStructuredData } from "@/src/components/PageStructuredData";

import { buildComparisonPage } from "../comparison/comparisonMetadata";
import { VsIndividualApisComparison } from "./content";

const {
  comparison,
  metadata: pageMetadata,
  structuredData,
} = buildComparisonPage("vs-individual-apis");

export const metadata = pageMetadata;

export default function GraphQLFederationVsIndividualApisPage() {
  return (
    <>
      <PageStructuredData {...structuredData} />
      <VsIndividualApisComparison title={comparison.title} />
    </>
  );
}
