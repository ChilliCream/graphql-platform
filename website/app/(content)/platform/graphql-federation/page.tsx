import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";

import { ExplainerPage } from "./ExplainerPage";

const PAGE = {
  title: "What is GraphQL Federation?",
  description:
    "GraphQL federation combines the schemas of multiple independent services into one unified graph served at a single endpoint. Learn how composition, entities, and query planning work.",
  path: "/platform/graphql-federation",
} as const;

export const metadata = pageMetadata(PAGE);

export default function GraphQLFederationPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        breadcrumbs={[
          { name: "Home", path: "/" },
          { name: "Platform", path: "/platform" },
          { name: "GraphQL Federation" },
        ]}
      />
      <ExplainerPage />
    </>
  );
}
