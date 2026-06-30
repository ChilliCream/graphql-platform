import type { Metadata } from "next";

import { DeliveryFormatsSection } from "@/src/components/training/DeliveryFormatsSection";
import { FunBand } from "@/src/components/training/FunBand";
import { LevelsSection } from "@/src/components/training/LevelsSection";
import { OffersSection } from "@/src/components/training/OffersSection";
import { OutcomesSection } from "@/src/components/training/OutcomesSection";
import { TrainingClosingCta } from "@/src/components/training/TrainingClosingCta";
import { TrainingFaq } from "@/src/components/training/TrainingFaq";
import { TrainingHero } from "@/src/components/training/TrainingHero";
import { pageMetadata } from "@/src/helpers/pageMetadata";

const META_DESCRIPTION =
  "Book GraphQL training for your team. Hot Chocolate, ASP.NET Core, React, and Relay curriculum that flexes for beginner, mixed, and advanced engineering teams.";

export const metadata: Metadata = {
  ...pageMetadata({
    title: "GraphQL Training for Your Team",
    description: META_DESCRIPTION,
    path: "/services/training",
  }),
  keywords: [
    "GraphQL training",
    "Hot Chocolate training",
    "corporate GraphQL workshop",
    "team GraphQL training",
    "Relay training",
    "ASP.NET Core GraphQL training",
  ],
};

export default function TrainingPage() {
  return (
    <>
      <TrainingHero />
      <LevelsSection />
      <OffersSection />
      <OutcomesSection />
      <DeliveryFormatsSection />
      <FunBand />
      <TrainingFaq />
      <TrainingClosingCta />
    </>
  );
}
