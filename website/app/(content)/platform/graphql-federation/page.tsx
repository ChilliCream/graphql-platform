import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  LOGO_ID,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { ExplainerPage } from "./ExplainerPage";
import { FEDERATION_DEFINITION } from "./terms";

const PAGE = {
  title: "GraphQL Federation: what, why, and how",
  description:
    "A plain-language guide to GraphQL Federation: what it is, why teams adopt it, how composition, entities and the gateway work, and whether it fits your setup.",
  path: "/platform/graphql-federation",
  keywords: [
    "graphql federation",
    "what is graphql federation",
    "graphql federation explained",
    "graphql federation vs schema stitching",
    "graphql federation vs bff",
    "graphql federation vs apollo federation",
    "graphql federation vs monolith",
    "apollo federation vs graphql federation",
    "graphql federation specification",
    "graphql federation gateway",
    "graphql federation directives",
    "graphql federation example",
    "composite schema",
    "subgraph",
    "source schema",
    "schema composition",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const TERM_ID = schemaId(PAGE.path, "graphql-federation");
const ARTICLE_ID = schemaId(PAGE.path, "article");

const GRAPHQL_FEDERATION_TERM = {
  "@type": "DefinedTerm",
  "@id": TERM_ID,
  name: "GraphQL Federation",
  description: FEDERATION_DEFINITION,
  sameAs: [
    "https://graphql.github.io/graphql-federation-spec/draft/",
    "https://graphql.org/learn/federation/",
  ],
};

const ARTICLE = {
  "@type": "TechArticle",
  "@id": ARTICLE_ID,
  headline: PAGE.title,
  description: PAGE.description,
  inLanguage: "en",
  image: schemaRef(LOGO_ID),
  publisher: schemaRef(ORGANIZATION_ID),
  mainEntityOfPage: schemaRef(schemaId(PAGE.path, "webpage")),
  about: schemaRef(TERM_ID),
};

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
        mainEntity={schemaRef(ARTICLE_ID)}
        about={schemaRef(TERM_ID)}
        additionalNodes={[ARTICLE, GRAPHQL_FEDERATION_TERM]}
      />
      <ExplainerPage />
    </>
  );
}
