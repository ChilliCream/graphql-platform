import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "About ChilliCream",
  description:
    "About ChilliCream: the GraphQL platform for .NET teams dealt as a six-card deck, from the Hot Chocolate server to the Nitro control plane, open source and in the open.",
  keywords: [
    "ChilliCream",
    "About ChilliCream",
    "GraphQL platform",
    "Hot Chocolate",
    "Nitro",
    "Strawberry Shake",
    ".NET GraphQL",
    "Fusion",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "About ChilliCream",
    description:
      "We build the end-to-end GraphQL platform for .NET teams, dealt as one hand of six products, open source and in the open on GitHub.",
  },
};

export default function AboutPreviewV8Page() {
  return <ClientPage />;
}
