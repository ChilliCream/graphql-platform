import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import Tokamak from "../_prototypes/heroes/Tokamak";

export const metadata: Metadata = {
  title: "Prototype v12 - Tokamak",
  description:
    "A coral plasma torus of orbiting streaks and a twisting filament inside a tiled steel chamber behind the Fusion hero copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV12Page() {
  return (
    <PrototypeShell version={12}>
      <FusionPage hero={<HeroShell art={<Tokamak />} />} />
    </PrototypeShell>
  );
}
