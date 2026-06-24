import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL Schemas | ChilliCream",
  description:
    "GraphQL release safety: the registry stamps every schema change safe, dangerous, or breaking, then a CI gate stops unsafe releases before any client ships.",
  keywords: [
    "GraphQL release safety",
    "schema diff review",
    "breaking change detection",
    "schema registry",
    "client registry",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "schema version timeline",
    "GraphQL contract review",
  ],
  openGraph: {
    title: "Deal Every Release a Verdict",
    description:
      "A stacked deck of safe, dangerous, and breaking verdicts for every schema change, dealt against your published clients before a release ships.",
  },
  robots: { index: false, follow: false },
};

export default function ReleaseSafetyPreviewV9() {
  return <ClientPage />;
}
