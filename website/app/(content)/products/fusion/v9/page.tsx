import type { Metadata } from "next";

import { Constellation } from "../_prototypes/concepts/Constellation/Constellation";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v9 - Constellation",
  description:
    "Prototype of the Fusion product page as a star system: the gateway is the star, subgraphs are planets on their orbits, clients are probes, and composition is the gravitational check that stops the build when orbits cross.",
  robots: { index: false, follow: false },
};

export default function PrototypeV9Page() {
  return (
    <PrototypeShell version={9}>
      <Constellation />
    </PrototypeShell>
  );
}
