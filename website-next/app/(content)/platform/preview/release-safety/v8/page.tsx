import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Release Safety for GraphQL Schemas | ChilliCream",
  description:
    "GraphQL release safety as a punched ticket. Every schema change is stamped safe, dangerous, or breaking, and CI voids breaking tickets before they board.",
  keywords: [
    "GraphQL release safety",
    "schema change ticket",
    "schema diff review",
    "breaking change detection",
    "schema registry",
    "client registry",
    "validate publish gate",
    "CI schema checks",
    "published clients affected",
    "schema version history",
  ],
  openGraph: {
    title: "Stamp Every Schema Change Before It Boards",
    description:
      "A perforated validation ticket for every schema change, classified safe, dangerous, or breaking, with the CI gate voiding breaking tickets at the door.",
  },
  robots: { index: false, follow: false },
};

export default function ReleaseSafetyPreviewV8() {
  return <ClientPage />;
}
