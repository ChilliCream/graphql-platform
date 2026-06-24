import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Build Loop: Contour Survey of One Source",
  description:
    "Implementation-first GraphQL in C#. Schema, DataLoader batching, and a typed Strawberry Shake .NET client are surveyed from one annotated class, so nothing drifts.",
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
    title: "Contour Survey of One Source",
    description:
      "One annotated C# class sits at the summit. The schema, resolver pipeline, DataLoaders, and a typed .NET client read off it like contour lines on a survey map.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
