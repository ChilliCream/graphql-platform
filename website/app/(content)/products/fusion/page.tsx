import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { FusionPage } from "./FusionPage";

const PAGE = {
  title: "Fusion: GraphQL Federation Gateway",
  description:
    "Fusion is the GraphQL Federation gateway that composes source schemas from any GraphQL server, OpenAPI, and gRPC into one composite schema and executes every query across them.",
  path: "/products/fusion",
  keywords: [
    "GraphQL Federation gateway",
    "GraphQL gateway",
    "Apollo Federation compatible gateway",
    "composite schema",
    "Fusion GraphQL",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Fusion",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  codeRepository: "https://github.com/ChilliCream/graphql-platform",
  programmingLanguage: {
    "@type": "ComputerLanguage",
    name: "C#",
  },
  runtimePlatform: ".NET and ASP.NET Core",
  license: "https://opensource.org/license/mit",
  creator: schemaRef(ORGANIZATION_ID),
  publisher: schemaRef(ORGANIZATION_ID),
} as const;

export default function FusionProductPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Fusion" }]}
        mainEntity={schemaRef(SOFTWARE_ID)}
        about={schemaRef(SOFTWARE_ID)}
        additionalNodes={[SOFTWARE]}
      />
      <FusionPage />
    </>
  );
}
