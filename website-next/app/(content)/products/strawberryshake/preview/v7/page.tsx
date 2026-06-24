import type { Metadata } from "next";

import { StrawberryShakeV7Client } from "./ClientPage";

// Next.js does not allow `export const metadata` from a Client Component
// module, so this server-component page exports the metadata and renders
// the client subtree where the motion hooks live.
export const metadata: Metadata = {
  title: "Strawberry Shake: Typed GraphQL Client for .NET",
  description:
    "Strawberry Shake is the open-source typed GraphQL client for .NET. MSBuild codegen emits typed records, a normalized store, and WebSocket subscriptions.",
  keywords: [
    "Strawberry Shake",
    "typed GraphQL client for .NET",
    "MSBuild codegen",
    "Blazor GraphQL",
    "Razor GraphQL",
    "MAUI GraphQL",
    "GraphQL subscriptions",
    "persisted operations",
    "reactive store",
    "Hot Chocolate",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Strawberry Shake: Typed GraphQL Client for .NET",
    description:
      "An animated MSBuild forge: .graphql in, typed C# records out, normalized store, live subscriptions. MIT-licensed.",
    type: "website",
  },
};

export default function Page() {
  return <StrawberryShakeV7Client />;
}
