import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { SixthRail } from "../_prototypes/concepts/SixthRail";

export const metadata: Metadata = {
  title: "Prototype v1 - The Sixth Rail",
  description:
    "Prototype of the federation problem section: the composite schema drawn as a sixth rail born at the composition hub, not a merge of the five service lines.",
  robots: { index: false, follow: false },
};

export default function PrototypeV1Page() {
  return (
    <PrototypeShell version={1}>
      <SixthRail />
    </PrototypeShell>
  );
}
