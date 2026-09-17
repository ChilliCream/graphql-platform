import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import PlasmaFusion from "../_prototypes/heroes/PlasmaFusion";

export const metadata: Metadata = {
  title: "Prototype v11 - Plasma Fusion",
  description:
    "Two spheres of branching plasma filaments fusing into a white-hot core behind the Fusion hero copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV11Page() {
  return (
    <PrototypeShell version={11}>
      <FusionPage hero={<HeroShell art={<PlasmaFusion />} />} />
    </PrototypeShell>
  );
}
