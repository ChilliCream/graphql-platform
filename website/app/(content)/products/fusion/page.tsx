import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  FEDERATION_PLATFORM_GROUP,
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { FusionPage } from "./FusionPage";

const PAGE = {
  title: "Fusion: GraphQL Federation Router (Gateway)",
  description:
    "Fusion is a high-performance GraphQL Federation router (gateway) for connecting GraphQL, REST, and gRPC APIs at scale, with native support for GraphQL Federation and Apollo Federation.",
  path: "/products/fusion",
  keywords: [
    "GraphQL Federation router",
    "GraphQL Federation gateway",
    "GraphQL router",
    "GraphQL gateway",
    "Apollo Federation compatible router",
    "Apollo Federation router",
    "Apollo Federation gateway",
    "coherent graph",
    "Fusion GraphQL",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Fusion",
  alternateName: "Fusion GraphQL Federation Gateway",
  description: PAGE.description,
  keywords: [
    "GraphQL Federation router",
    "GraphQL Federation gateway",
    "GraphQL router",
    "GraphQL gateway",
    "Apollo Federation router",
    "Apollo Federation gateway",
  ],
  url: toAbsoluteUrl(PAGE.path),
  codeRepository: "https://github.com/ChilliCream/graphql-platform",
  programmingLanguage: {
    "@type": "ComputerLanguage",
    name: "C#",
  },
  runtimePlatform: ".NET and ASP.NET Core",
  license: "https://opensource.org/license/mit",
  isPartOf: schemaRef(FEDERATION_PLATFORM_GROUP.id),
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
