import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Hot Chocolate: GraphQL Server for .NET",
  description:
    "Hot Chocolate is the open-source GraphQL server for .NET. Source-generated C# resolvers, batched DataLoaders, subscriptions, OpenTelemetry, and Fusion ready.",
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
      "A field journal for shipping GraphQL on .NET. Source-generated resolvers, batched DataLoaders, subscriptions, OpenTelemetry, Fusion ready. MIT licensed.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
