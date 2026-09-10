import { PageStructuredData } from "@/src/components/PageStructuredData";
import { pageMetadata } from "@/src/helpers/pageMetadata";
import { toAbsoluteUrl } from "@/src/helpers/siteUrl";
import {
  ORGANIZATION_ID,
  schemaId,
  schemaRef,
} from "@/src/helpers/structuredData";

import { ClientPage } from "./ClientPage";

const PAGE = {
  title: "Mocha: Messaging Framework for .NET",
  description:
    "Mocha is a .NET messaging framework with a source-generated mediator for in-process work and a message bus for commands and events between services.",
  path: "/products/mocha",
  keywords: [
    ".NET messaging framework",
    ".NET message bus",
    "C# mediator",
    "distributed messaging .NET",
    "Mocha messaging",
  ],
} as const;

export const metadata = pageMetadata(PAGE);

const SOFTWARE_ID = schemaId(PAGE.path, "software");
const SOFTWARE = {
  "@type": "SoftwareSourceCode",
  "@id": SOFTWARE_ID,
  name: "Mocha",
  description: PAGE.description,
  url: toAbsoluteUrl(PAGE.path),
  codeRepository: "https://github.com/ChilliCream/graphql-platform",
  programmingLanguage: {
    "@type": "ComputerLanguage",
    name: "C#",
  },
  runtimePlatform: ".NET",
  creator: schemaRef(ORGANIZATION_ID),
  publisher: schemaRef(ORGANIZATION_ID),
} as const;

export default function MochaPage() {
  return (
    <>
      <PageStructuredData
        title={PAGE.title}
        description={PAGE.description}
        path={PAGE.path}
        pageType="ItemPage"
        breadcrumbs={[{ name: "Home", path: "/" }, { name: "Mocha" }]}
        mainEntity={schemaRef(SOFTWARE_ID)}
        about={schemaRef(SOFTWARE_ID)}
        additionalNodes={[SOFTWARE]}
      />
      <ClientPage />
    </>
  );
}
