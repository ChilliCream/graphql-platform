import type { Metadata } from "next";

import { PinnedConstellationStage } from "../_prototypes/concepts/PinnedConstellationStage";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v5 - Pinned Constellation Stage",
  description:
    "Prototype of the federation problem section: five service marks pinned at fixed anchors while a sticky stage stages the merge as paper collation, not a shrinking line count.",
  robots: { index: false, follow: false },
};

export default function PrototypeV5Page() {
  return (
    <PrototypeShell version={5}>
      <PinnedConstellationStage />
    </PrototypeShell>
  );
}
