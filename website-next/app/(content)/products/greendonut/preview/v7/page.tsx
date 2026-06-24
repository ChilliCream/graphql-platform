import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page exports the metadata and renders
// the client subtree where the motion hooks live.
export const metadata: Metadata = {
  title: "Green Donut: DataLoader for .NET",
  description:
    "Green Donut is the DataLoader for .NET. Watch six resolver requests coalesce on one tick into a single batched fetch with per-request caching and key dedup.",
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
    title: "Green Donut: DataLoader for .NET",
    description:
      "Watch six resolver requests coalesce on one tick into a single batched fetch. Batching, per-request caching, dedup, and the [DataLoader] attribute generate the wiring for you. MIT-licensed.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
