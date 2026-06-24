import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Build Loop: Codegen Forge",
  description:
    "Implementation-first GraphQL .NET. One annotated C# class forges an SDL schema, a batched DataLoader, and a typed Strawberry Shake .NET client in motion.",
  keywords: [
    "implementation-first GraphQL .NET",
    "Hot Chocolate source generation",
    "Strawberry Shake MSBuild codegen",
    "C# GraphQL schema",
    "DataLoader batching",
    "QueryType attribute",
    "generated GraphQL SDL",
    "typed end to end GraphQL",
  ],
  openGraph: {
    title: "Codegen Forge",
    description:
      "An annotated C# class is the spark. Watch SDL, a DataLoader batch, and a typed .NET client forge themselves in real time.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
