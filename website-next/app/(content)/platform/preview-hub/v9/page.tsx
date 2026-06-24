import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

/* -------------------------------------------------------------------------- */
/*  Metadata                                                                  */
/*  Hatched Atlas variant. Sections stitched by 45deg diagonal hatch bands    */
/*  rather than spaced by gaps. Server component owns metadata, the client    */
/*  module owns the motion hooks (useReducedMotion + whileInView, once).     */
/* -------------------------------------------------------------------------- */

export const metadata: Metadata = {
  title: "ChilliCream Platform: Hatched Atlas",
  description:
    "Explore the ChilliCream GraphQL platform as a hatched atlas: eight surfaces and the Nitro control plane woven into one cloth across Build, Run, and Evolve.",
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
    title: "ChilliCream Platform: Hatched Atlas",
    description:
      "Eight platform surfaces and the Nitro control plane stitched into one woven cloth across Build, Run, and Evolve.",
  },
  robots: { index: false, follow: false },
};

/* -------------------------------------------------------------------------- */
/*  Page                                                                      */
/* -------------------------------------------------------------------------- */

export default function PlatformHatchedAtlasPage() {
  return <ClientPage />;
}
