import type { Metadata } from "next";

import { FromOurBlog } from "@/src/components/FromOurBlog";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "ChilliCream: GraphQL platform for .NET",
  description:
    "Constellation Grid: a dot-grid lattice for the ChilliCream GraphQL platform on .NET. Hot Chocolate, Strawberry Shake, Nitro, Fusion, Mocha, Green Donut.",
  keywords: [
    "ChilliCream",
    "GraphQL platform for .NET",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    "Fusion",
    "Mocha",
    "Green Donut",
    "schema registry",
    "GraphQL observability",
  ],
  openGraph: {
    title: "ChilliCream: GraphQL platform for .NET",
    description:
      "A coherent fabric: dot-grid lattice ties Hot Chocolate, Strawberry Shake, Nitro, Fusion, Mocha, and Green Donut into one GraphQL platform for .NET.",
  },
  robots: { index: false, follow: false },
};

export default function LandingPreviewV8Page() {
  return (
    <ClientPage
      blogSlot={
        <section className="py-16 sm:py-20">
          <FromOurBlog />
        </section>
      }
    />
  );
}
