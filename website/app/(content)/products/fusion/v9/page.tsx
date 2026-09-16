import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import OrbitalRings from "../_prototypes/heroes/OrbitalRings";

export const metadata: Metadata = {
  title: "Prototype v9 - Orbital Rings",
  description:
    "Hero concept that gyrates five tilted rings in the service colours around one bright core behind the copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV9Page() {
  return (
    <PrototypeShell version={9}>
      <FusionPage hero={<HeroShell art={<OrbitalRings />} />} />
    </PrototypeShell>
  );
}
