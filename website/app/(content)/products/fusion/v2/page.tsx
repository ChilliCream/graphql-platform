import type { Metadata } from "next";

import { Harbour } from "../_prototypes/concepts/Harbour/Harbour";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v2 - Harbour",
  description:
    "Prototype of the Fusion product page as a harbour at dusk: ships dock at one pier, warehouses fly a language flag and a specification pennant, and every manifest passes the customs check before departure.",
  robots: { index: false, follow: false },
};

export default function PrototypeV2Page() {
  return (
    <PrototypeShell version={2}>
      <Harbour />
    </PrototypeShell>
  );
}
