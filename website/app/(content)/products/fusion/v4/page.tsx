import type { Metadata } from "next";

import { Orchestra } from "../_prototypes/concepts/Orchestra/Orchestra";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v4 - Orchestra",
  description:
    "Concept prototype of the Fusion product page: the gateway as conductor, every subgraph a section of the orchestra, and the composite schema as one piece the hall hears.",
  robots: { index: false, follow: false },
};

export default function PrototypeV4Page() {
  return (
    <PrototypeShell version={4}>
      <Orchestra />
    </PrototypeShell>
  );
}
