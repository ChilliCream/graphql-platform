import { PageStructuredData } from "@/src/components/PageStructuredData";
import {
  createNitroOfferNodes,
  createNitroProductNode,
  NITRO_PRODUCT_ID,
} from "@/src/helpers/nitroStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { schemaRef } from "@/src/helpers/structuredData";

import { ClientPage } from "./ClientPage";

const PAGE = {
  title: "Nitro: GraphQL Observability Platform",
  description:
    "Nitro is the API operations platform for observability, OpenTelemetry tracing, schema governance, client safety, and GraphQL release checks in one control plane.",
  path: "/products/nitro",
  keywords: [
    "GraphQL observability platform",
    "GraphQL schema governance",
    "GraphQL schema registry",
    "OpenTelemetry GraphQL tracing",
    "GraphQL release checks",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const OFFERS = createNitroOfferNodes();
const PRODUCT = createNitroProductNode(PAGE.description, OFFERS);

export default function NitroPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Nitro" }]}
        mainEntity={schemaRef(NITRO_PRODUCT_ID)}
        about={schemaRef(NITRO_PRODUCT_ID)}
        additionalNodes={[PRODUCT, ...OFFERS]}
      />
      <ClientPage />
    </>
  );
}
