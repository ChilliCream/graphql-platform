import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Dewey Decimal DataLoader, Green Donut for .NET",
  description:
    "Green Donut is the open-source DataLoader for .NET. Catalog your N+1 away with batching, per-request caching, dedup, and source-generated wiring. MIT licensed.",
  keywords: [
    "Green Donut",
    "DataLoader",
    ".NET DataLoader",
    "N+1 problem",
    "GraphQL batching",
    "request scoped cache",
    "Hot Chocolate",
    "C# resolvers",
    "AOT-friendly",
    "ChilliCream",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Dewey Decimal DataLoader, Green Donut for .NET",
    description:
      "Kill N+1 in your .NET resolvers. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT-licensed.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
