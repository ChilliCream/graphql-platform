import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "The Registry Codex, GraphQL Schema CI | ChilliCream",
  description:
    "A field guide to GraphQL schema registry CI with Nitro: validate, upload, publish, and promote schema changes across dev, QA, and prod without breaking clients.",
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
    title: "The Registry Codex, GraphQL Schema CI",
    description:
      "A field guide to safe schema evolution. Validate, upload, publish, and promote through the Nitro CLI on any runner.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
