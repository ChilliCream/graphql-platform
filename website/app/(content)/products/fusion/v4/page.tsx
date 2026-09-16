import type { Metadata } from "next";

import { FusionPage } from "../FusionPage";
import { HeroShell } from "../_prototypes/HeroShell";
import { PrototypeShell } from "../_prototypes/PrototypeShell";
import Confluence from "../_prototypes/heroes/Confluence";

export const metadata: Metadata = {
  title: "Prototype v4 - Confluence",
  description:
    "Five service-coloured tributaries wind through contour lines and merge into one broad white channel behind the Fusion hero copy.",
  robots: { index: false, follow: false },
};

export default function PrototypeV4Page() {
  return (
    <PrototypeShell version={4}>
      <FusionPage hero={<HeroShell art={<Confluence />} />} />
    </PrototypeShell>
  );
}
