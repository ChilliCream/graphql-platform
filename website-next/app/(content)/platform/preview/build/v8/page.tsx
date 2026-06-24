import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Build Loop: Constellation of Truth",
  description:
    "Implementation-first GraphQL in C#. One annotated class is the source star: the schema, DataLoader, and a typed Strawberry Shake .NET client all trace back.",
  keywords: [
    "implementation-first GraphQL",
    "Hot Chocolate source generation",
    "C# GraphQL schema",
    "Strawberry Shake typed client",
    "DataLoader batching",
    "QueryType attribute",
    "generated GraphQL SDL",
    "no schema drift",
    "typed end to end GraphQL",
    ".NET GraphQL build loop",
  ],
  openGraph: {
    title: "Constellation of Truth",
    description:
      "One annotated C# class is the source star. The schema, resolver pipeline, DataLoaders, and a typed .NET client are the satellites that trace back to it.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
