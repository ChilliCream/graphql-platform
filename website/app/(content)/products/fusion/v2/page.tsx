import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import SpectrumBands from "../_prototypes/heroes/SpectrumBands";

export const metadata: Metadata = {
  title: "Prototype v2 - Spectrum Bands",
  description:
    "Five service-coloured bands converge behind the Fusion hero copy and merge into a single white line.",
  robots: { index: false, follow: false },
};

export default function PrototypeV2Page() {
  return (
    <PrototypeShell version={2}>
      <FusionPage hero={<HeroShell art={<SpectrumBands />} />} />
    </PrototypeShell>
  );
}
