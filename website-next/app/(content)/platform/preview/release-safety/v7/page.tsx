import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL Schemas | ChilliCream",
  description:
    "Safe GraphQL schema evolution, scrubbable. Classify every change safe, dangerous, or breaking, then a CI gate stops unsafe releases before clients ship.",
  keywords: [
    "safe GraphQL schema evolution",
    "GraphQL release safety",
    "schema diff review",
    "breaking change detection",
    "schema registry",
    "client registry",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "schema version timeline",
  ],
  openGraph: {
    title: "Scrub Through the Schema Diff",
    description:
      "A scroll-driven schema diff that classifies safe, dangerous, and breaking, then snaps a CI gate shut and reveals the published clients affected.",
  },
  robots: { index: false, follow: false },
};

export default function ReleaseSafetyPreviewV7() {
  return <ClientPage />;
}
