import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import IsometricStack from "../_prototypes/heroes/IsometricStack";

export const metadata: Metadata = {
  title: "Prototype v5 - Isometric Stack",
  description:
    "Five service-coloured isometric slabs slide in from the right and settle into one composite block behind the Fusion hero copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV5Page() {
  return (
    <PrototypeShell version={5}>
      <FusionPage hero={<HeroShell art={<IsometricStack />} />} />
    </PrototypeShell>
  );
}
