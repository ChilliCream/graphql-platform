import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Pipeline Pulse, GraphQL Schema Registry CI | ChilliCream",
  description:
    "GraphQL schema registry CI you can watch: validate, upload, publish, deploy across dev, QA, staging, and prod with breaking-change classification before promotion.",
  keywords: [
    "GraphQL schema registry CI",
    "Nitro CLI",
    "schema registry",
    "client registry",
    "breaking change detection",
    "validate publish gate",
    "environment workflows",
    "GitHub Actions GraphQL",
    "Azure DevOps GraphQL",
    "schema evolution pipeline",
  ],
  openGraph: {
    title: "Pipeline Pulse, GraphQL Schema Registry CI",
    description:
      "A scroll-driven pipeline for safe schema evolution. Validate, upload, publish, deploy through the Nitro CLI on any runner.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
