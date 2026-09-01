import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import {
  createFaqNode,
  LOGO_ID,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { ExplainerPage } from "./ExplainerPage";
import { FEDERATION_FAQ_ITEMS } from "./faq";
import { FEDERATION_DEFINITION, FEDERATION_TERMS } from "./terms";

const PAGE = {
  title: "What is GraphQL Federation?",
  description:
    "GraphQL Federation lets each team own a GraphQL service while clients see one API. A plain-language guide to composition, entities, lookups and the open spec.",
  path: "/platform/graphql-federation",
  keywords: [
    "graphql federation",
    "what is graphql federation",
    "graphql federation explained",
    "graphql federation vs schema stitching",
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
const GLOSSARY_ID = schemaId(PAGE.path, "glossary");
const ARTICLE_ID = schemaId(PAGE.path, "article");

const GRAPHQL_FEDERATION_TERM = {
  "@type": "DefinedTerm",
  "@id": TERM_ID,
  name: "GraphQL Federation",
  description: FEDERATION_DEFINITION,
  sameAs: [
    "https://graphql.github.io/composite-schemas-spec/draft/",
    "https://graphql.org/learn/federation/",
  ],
};

const GLOSSARY = {
  "@type": "DefinedTermSet",
  "@id": GLOSSARY_ID,
  name: "GraphQL Federation terminology",
  hasDefinedTerm: FEDERATION_TERMS.map(({ term, meaning }) => ({
    "@type": "DefinedTerm",
    name: term,
    description: meaning,
    inDefinedTermSet: schemaRef(GLOSSARY_ID),
  })),
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

const FAQ = createFaqNode(PAGE.path, FEDERATION_FAQ_ITEMS);

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
        additionalNodes={[ARTICLE, GRAPHQL_FEDERATION_TERM, GLOSSARY, FAQ]}
      />
      <ExplainerPage />
    </>
  );
}
