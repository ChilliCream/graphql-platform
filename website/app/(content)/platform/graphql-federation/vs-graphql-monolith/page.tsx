import { PageStructuredData } from "@/src/components/PageStructuredData";

import { buildComparisonPage } from "../comparison/comparisonMetadata";
import { VsGraphQLMonolithComparison } from "./content";

const {
  comparison,
  metadata: pageMetadata,
  structuredData,
} = buildComparisonPage("vs-graphql-monolith");

export const metadata = pageMetadata;

export default function GraphQLFederationVsGraphQLMonolithPage() {
  return (
    <>
      <PageStructuredData {...structuredData} />
      <VsGraphQLMonolithComparison title={comparison.title} />
    </>
  );
}
