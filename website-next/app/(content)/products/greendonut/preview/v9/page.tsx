import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET, in conversation",
  description:
    "Green Donut is the open-source DataLoader for .NET. A standup thread on killing N+1 with batching, per-request caching, dedup, and source-generated wiring. MIT.",
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
    title: "Green Donut: DataLoader for .NET, in conversation",
    description:
      "A recorded standup between Dev, Agent, and Ops on killing N+1 in a .NET service. Batching, per-request caching, dedup, and the [DataLoader] attribute. MIT.",
    type: "website",
  },
};

export default function GreenDonutV9Page() {
  return <ClientPage />;
}
