import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import ParticleStream from "../_prototypes/heroes/ParticleStream";

export const metadata: Metadata = {
  title: "Prototype v10 - Particle Stream",
  description:
    "Hero concept that streams particles in the service colours toward one attractor behind the copy and out as a white river.",
  robots: { index: false, follow: false },
};

export default function PrototypeV10Page() {
  return (
    <PrototypeShell version={10}>
      <FusionPage hero={<HeroShell art={<ParticleStream />} />} />
    </PrototypeShell>
  );
}
