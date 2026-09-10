import type { Metadata } from "next";

import { IsometricCity } from "../_prototypes/concepts/IsometricCity/IsometricCity";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v6 - Isometric City",
  description:
    "Prototype of the Fusion product page as an isometric city block: the gateway is the plaza with one door, subgraphs are buildings with language signage and a specification flag, composition is the zoning inspection and Nitro is the traffic camera log.",
  robots: { index: false, follow: false },
};

export default function PrototypeV6Page() {
  return (
    <PrototypeShell version={6}>
      <IsometricCity />
    </PrototypeShell>
  );
}
