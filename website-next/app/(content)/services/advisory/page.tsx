import type { Metadata } from "next";

import { AdvisoryFaq } from "@/src/components/advisory/AdvisoryFaq";
import { AdvisoryHero } from "@/src/components/advisory/AdvisoryHero";
import { ContactBand } from "@/src/components/advisory/ContactBand";
import { EngagementStrip } from "@/src/components/advisory/EngagementStrip";
import { TeamSection } from "@/src/components/advisory/TeamSection";
import { TierGrid } from "@/src/components/advisory/TierGrid";

export const metadata: Metadata = {
  title: "GraphQL Advisory by ChilliCream",
  description:
    "GraphQL consulting in packages of hours, or full contracting, from the team behind Hot Chocolate, Fusion, and Nitro. Talk to an expert about your project.",
  keywords: [
    "GraphQL advisory",
    "GraphQL consulting",
    "GraphQL contracting",
    "Hot Chocolate consulting",
    "Fusion consulting",
    "Nitro consulting",
    "ChilliCream advisory",
  ],
  openGraph: {
    title: "GraphQL Advisory by ChilliCream",
    description:
      "Consulting in packages of hours, or full contracting, from the engineers behind Hot Chocolate, Fusion, and Nitro. Talk to us about your project.",
  },
};

export default function AdvisoryPage() {
  return (
    <>
      <AdvisoryHero />
      <TierGrid />
      <EngagementStrip />
      <TeamSection />
      <AdvisoryFaq />
      <ContactBand />
    </>
  );
}
