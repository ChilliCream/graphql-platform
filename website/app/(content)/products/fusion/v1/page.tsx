import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import Prism from "../_prototypes/heroes/Prism";

export const metadata: Metadata = {
  title: "Prototype v1 - Prism",
  description:
    "A glass prism splits a white beam into five service-coloured rays that fan across the Fusion hero.",
  robots: { index: false, follow: false },
};

export default function PrototypeV1Page() {
  return (
    <PrototypeShell version={1}>
      <FusionPage hero={<HeroShell art={<Prism />} />} />
    </PrototypeShell>
  );
}
