import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

/* -------------------------------------------------------------------------- */
/*  Metadata                                                                  */
/*  Server component shell. All interactive logic lives in ClientPage.        */
/* -------------------------------------------------------------------------- */

export const metadata: Metadata = {
  title: "ChilliCream Platform: The Spectrum Hinge",
  description:
    "ChilliCream GraphQL platform overview: eight numbered capabilities across Build, Run, and Evolve that meet the Nitro control plane in one spectrum hinge.",
  keywords: [
    "ChilliCream platform",
    "GraphQL platform overview",
    "GraphQL build pipeline",
    "GraphQL observability",
    "GraphQL workflows",
    "GraphQL release safety",
    "GraphQL analytics",
    "continuous integration",
    "GraphQL ecosystem",
    "Nitro control plane",
  ],
  openGraph: {
    title: "ChilliCream Platform: The Spectrum Hinge",
    description:
      "ChilliCream GraphQL platform overview: eight numbered capabilities across Build, Run, and Evolve that meet the Nitro control plane in one spectrum hinge.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformSpectrumHingePage() {
  return <ClientPage />;
}
