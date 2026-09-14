import type { Metadata } from "next";

import { Airport } from "../_prototypes/concepts/Airport/Airport";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v5 - Airport",
  description:
    "Prototype of the Fusion product page as an airport: a split-flap departures board over the apron, gates for the source schemas, clearance before takeoff and the Nitro boarding check.",
  robots: { index: false, follow: false },
};

export default function PrototypeV5Page() {
  return (
    <PrototypeShell version={5}>
      <Airport />
    </PrototypeShell>
  );
}
