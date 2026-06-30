import type { Metadata } from "next";

import { ClosingCta } from "@/src/components/support/ClosingCta";
import { ComparisonMatrix } from "@/src/components/support/ComparisonMatrix";
import { EnterpriseBand } from "@/src/components/support/EnterpriseBand";
import { PlanGrid } from "@/src/components/support/PlanGrid";
import { SupportFaq } from "@/src/components/support/SupportFaq";
import { SupportHero } from "@/src/components/support/SupportHero";

export const metadata: Metadata = {
  title: "GraphQL Support Plans",
  description:
    "Support and response-time plans from ChilliCream. Next business day on Startup and Business, 24 hours for Enterprise criticals, direct to the engineers who build Hot Chocolate, Fusion, and Nitro.",
  keywords: [
    "GraphQL support",
    "HotChocolate support",
    "Nitro support",
    "ChilliCream support",
    "enterprise GraphQL",
    "incident response",
  ],
  openGraph: {
    type: "website",
    title: "GraphQL Support Plans",
    description:
      "Response times you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus a direct line to the core team.",
  },
  twitter: {
    card: "summary_large_image",
    title: "GraphQL Support Plans",
    description:
      "Response times you can hold us to. Next business day on Startup and Business, 24 hours for Enterprise criticals, plus a direct line to the core team.",
  },
};

export default function SupportPage() {
  return (
    <>
      <SupportHero />
      <PlanGrid />
      <ComparisonMatrix />
      <SupportFaq />
      <EnterpriseBand />
      <ClosingCta />
    </>
  );
}
