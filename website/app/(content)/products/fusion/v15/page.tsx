import type { Metadata } from "next";

import SequenceLanes from "../_prototypes/concepts/MissionControl/heroes/SequenceLanes";
import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v15 - Sequence Lanes",
  description:
    "Prototype of the Fusion product page with the hero drawn as a sequence diagram: a lifeline per client, the Fusion gateway and every source schema, with requests fanning out and merging back as time scrolls left.",
  robots: { index: false, follow: false },
};

export default function PrototypeV15Page() {
  return (
    <PrototypeShell version={15}>
      <MissionControl hero={<SequenceLanes />} />
    </PrototypeShell>
  );
}
