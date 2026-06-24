import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Fusion: Plan in Motion, Distributed GraphQL Gateway for .NET",
  description:
    "Fusion, the distributed GraphQL gateway for .NET. Watch composition at planning time, parallel and batched subgraph fetches, and one composed answer in motion.",
  robots: { index: false, follow: false },
  openGraph: {
    title: "Fusion: Distributed GraphQL Gateway for .NET",
    description:
      "Compose subgraphs into one validated graph at planning time. Apollo Federation spec compatible. Self-run on ASP.NET Core, built on Hot Chocolate.",
    type: "website",
  },
};

export default function Page() {
  return <ClientPage />;
}
