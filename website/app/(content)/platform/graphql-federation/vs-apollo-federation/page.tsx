import { PageStructuredData } from "@/src/components/PageStructuredData";

import { buildComparisonPage } from "../comparison/comparisonMetadata";
import { VsApolloFederationComparison } from "./content";

const {
  comparison,
  metadata: pageMetadata,
  structuredData,
} = buildComparisonPage("vs-apollo-federation");

export const metadata = pageMetadata;

export default function GraphQLFederationVsApolloFederationPage() {
  return (
    <>
      <PageStructuredData {...structuredData} />
      <VsApolloFederationComparison title={comparison.title} />
    </>
  );
}
