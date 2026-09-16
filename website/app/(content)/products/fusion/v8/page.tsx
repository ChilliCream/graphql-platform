import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import WaveformMix from "../_prototypes/heroes/WaveformMix";

export const metadata: Metadata = {
  title: "Prototype v8 - Waveform Mix",
  description:
    "Hero concept that scrolls the five service traces as an oscilloscope, summing them into one white composite behind the copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV8Page() {
  return (
    <PrototypeShell version={8}>
      <FusionPage hero={<HeroShell art={<WaveformMix />} />} />
    </PrototypeShell>
  );
}
