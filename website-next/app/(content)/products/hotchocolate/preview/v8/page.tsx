import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Hot Chocolate: The GraphQL Funnies for .NET",
  description:
    "Hot Chocolate is the open-source GraphQL server for .NET. C# resolvers, source-generated schema and DataLoaders, subscriptions, OpenTelemetry, and Fusion-ready.",
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
    title: "Hot Chocolate: The GraphQL Funnies for .NET",
    description:
      "C# is the schema. Source-generated resolvers, batched DataLoaders, subscriptions, OpenTelemetry, and Fusion compatibility. MIT-licensed.",
    type: "website",
  },
};

export default function HotChocolatePreviewV8() {
  return <ClientPage />;
}
