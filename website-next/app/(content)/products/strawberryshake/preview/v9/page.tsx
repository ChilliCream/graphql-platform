import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page exports the metadata and renders
// the client subtree where the motion + scroll-spy hooks live.
export const metadata: Metadata = {
  title: "Strawberry Shake: Typed GraphQL Client for .NET",
  description:
    "Strawberry Shake is the open-source typed GraphQL client for .NET. MSBuild codegen, EntityStore, fetch strategies, subscriptions, Blazor, Razor, MAUI ready.",
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "typed GraphQL client",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "EntityStore",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: Typed GraphQL Client for .NET",
    description:
      "Tag-first tour of the typed GraphQL client for .NET: .graphql, MSBuild codegen, EntityStore, strategies, subscriptions, Blazor, Razor, MAUI.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
