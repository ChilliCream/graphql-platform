import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import AuroraRibbons from "../_prototypes/heroes/AuroraRibbons";

export const metadata: Metadata = {
  title: "Prototype v7 - Aurora Ribbons",
  description:
    "Hero concept that braids the five service colours into one ribbon flowing behind the copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV7Page() {
  return (
    <PrototypeShell version={7}>
      <FusionPage hero={<HeroShell art={<AuroraRibbons />} />} />
    </PrototypeShell>
  );
}
