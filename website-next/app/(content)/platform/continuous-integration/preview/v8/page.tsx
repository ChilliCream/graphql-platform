import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Registry Wall, GraphQL Schema CI Board | ChilliCream",
  description:
    "A pinned mosaic for GraphQL CI: validate, upload, publish, and deploy through the Nitro CLI across dev, QA, staging, prod, with classification gates first.",
  keywords: [
    "GraphQL continuous integration",
    "GraphQL schema registry CI",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "breaking change detection",
    "validate publish gate",
    "environment workflows",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
  ],
  openGraph: {
    title: "Registry Wall, GraphQL Schema CI Board",
    description:
      "Every change pinned to the registry wall. Validate, upload, publish, deploy through the Nitro CLI on any runner.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
