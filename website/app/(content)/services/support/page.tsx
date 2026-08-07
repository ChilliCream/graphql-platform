import { ClosingCta } from "@/src/components/support/ClosingCta";
import { ComparisonMatrix } from "@/src/components/support/ComparisonMatrix";
import { EnterpriseBand } from "@/src/components/support/EnterpriseBand";
import { PlanGrid } from "@/src/components/support/PlanGrid";
import { SupportFaq } from "@/src/components/support/SupportFaq";
import { SupportHero } from "@/src/components/support/SupportHero";
import { pageMetadata } from "@/src/helpers/pageMetadata";

export const metadata = pageMetadata({
  title: "GraphQL Support Plans",
  description:
    "Support and response-time plans from ChilliCream. Next business day on Startup and Business, 24 hours for Enterprise criticals, direct to the engineers who build Hot Chocolate, Fusion, and Nitro.",
  path: "/services/support",
  keywords: [
    "GraphQL support",
    "HotChocolate support",
    "Nitro support",
    "ChilliCream support",
    "enterprise GraphQL",
    "incident response",
  ],
});

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
