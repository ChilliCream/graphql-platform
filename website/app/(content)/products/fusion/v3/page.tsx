import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { Switchboard } from "../_prototypes/concepts/Switchboard/Switchboard";

export const metadata: Metadata = {
  title: "Prototype v3 - Switchboard",
  description:
    "Prototype of the Fusion product page as a telephone exchange: client lines patched through one board into the subgraph lines, with two plug standards, adapter sleeves and a wiring check.",
  robots: { index: false, follow: false },
};

export default function PrototypeV3Page() {
  return (
    <PrototypeShell version={3}>
      <Switchboard />
    </PrototypeShell>
  );
}
