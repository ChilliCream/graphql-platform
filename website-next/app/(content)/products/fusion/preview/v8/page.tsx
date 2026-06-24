import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Fusion: Ridge Walk, Distributed GraphQL Gateway for .NET",
  description:
    "Fusion, ChilliCream's distributed GraphQL gateway. Walk the ridge from independent subgraphs to one composed .NET endpoint, proven answerable at planning time.",
  keywords: [
    "Fusion",
    "distributed GraphQL gateway",
    "GraphQL federation",
    "composite schema",
    "GraphQL Composite Schemas",
    "subgraph composition",
    "Apollo Federation",
    ".NET GraphQL gateway",
    "Hot Chocolate",
    "query plan",
    "satisfiability",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: Distributed GraphQL Gateway for .NET",
    description:
      "Compose subgraphs into one validated graph at planning time. Apollo Federation spec compatible. Self-run on ASP.NET Core, built on Hot Chocolate.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
