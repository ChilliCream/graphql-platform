import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import Loom from "../_prototypes/heroes/Loom";

export const metadata: Metadata = {
  title: "Prototype v6 - Loom",
  description:
    "Hero concept that weaves the five service colours into one fabric band trailing off into loose threads.",
  robots: { index: false, follow: false },
};

export default function PrototypeV6Page() {
  return (
    <PrototypeShell version={6}>
      <FusionPage hero={<HeroShell art={<Loom />} />} />
    </PrototypeShell>
  );
}
