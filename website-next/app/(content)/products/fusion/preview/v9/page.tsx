import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Fusion: Composition Calendar, GraphQL Gateway for .NET",
  description:
    "Fusion is ChilliCream's distributed GraphQL gateway. Compose subgraphs, validate satisfiability, plan, and execute, laid out as a planner calendar on .NET.",
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
    title: "Fusion: Composition Calendar, GraphQL Gateway for .NET",
    description:
      "Compose subgraphs into one validated graph at planning time. Apollo Federation spec compatible. Self-run on ASP.NET Core, built on Hot Chocolate.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
