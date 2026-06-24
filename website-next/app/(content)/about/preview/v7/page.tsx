import type { Metadata } from "next";

import { AboutV7Client } from "./AboutV7Client";

export const metadata: Metadata = {
  title: "About ChilliCream",
  description:
    "About ChilliCream: we build the end-to-end GraphQL platform for .NET teams, from Hot Chocolate to the Nitro control plane, open source, in the open on GitHub.",
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
      "We build the end-to-end GraphQL platform for .NET teams, from the Hot Chocolate server to the Nitro control plane, open source and in the open.",
  },
};

export default function AboutPreviewV7Page() {
  return <AboutV7Client />;
}
