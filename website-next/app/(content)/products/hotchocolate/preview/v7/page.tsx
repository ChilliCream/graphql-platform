import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Hot Chocolate: GraphQL Server for .NET",
  description:
    "Hot Chocolate is the open-source MIT GraphQL server for .NET. Watch C# turn into a typed GraphQL schema in one Roslyn source generator pass. Fusion ready.",
  keywords: [
    "Hot Chocolate",
    "GraphQL server",
    ".NET GraphQL",
    "C# GraphQL",
    "ASP.NET Core",
    "DataLoader",
    "Green Donut",
    "GraphQL subscriptions",
    "OpenTelemetry",
    "Apollo Federation",
    "Fusion",
    "Strawberry Shake",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Hot Chocolate: GraphQL Server for .NET",
    description:
      "Watch C# turn into a typed GraphQL schema in one Roslyn source generator pass. Source-generated resolvers, batched DataLoaders, subscriptions, OpenTelemetry, Fusion ready.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
