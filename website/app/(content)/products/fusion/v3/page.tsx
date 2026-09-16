import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import SubwayMap from "../_prototypes/heroes/SubwayMap";

export const metadata: Metadata = {
  title: "Prototype v3 - Subway Map",
  description:
    "Five service-coloured transit lines bend into a FUSION interchange behind the Fusion hero copy and continue on as a single line.",
  robots: { index: false, follow: false },
};

export default function PrototypeV3Page() {
  return (
    <PrototypeShell version={3}>
      <FusionPage hero={<HeroShell art={<SubwayMap />} />} />
    </PrototypeShell>
  );
}
