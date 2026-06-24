import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server component owns metadata and renders the client
// subtree where motion lives.
export const metadata: Metadata = {
  title: "Strawberry Shake: Field Postcards from the .NET Build",
  description:
    "Strawberry Shake is the strongly-typed GraphQL client for .NET: MSBuild codegen, typed records, a normalized reactive store, WebSocket subscriptions, Blazor.",
  keywords: [
    "Strawberry Shake",
    ".NET GraphQL client",
    "strongly-typed GraphQL",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "dotnet graphql",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: Field Postcards from the .NET Build",
    description:
      "Souvenirs from the Strawberry Shake pipeline: .graphql contracts, MSBuild codegen, typed records, normalized store, subscriptions, Blazor. MIT-licensed.",
    type: "website",
  },
};

export default function StrawberryShakePreviewV8() {
  return <ClientPage />;
}
