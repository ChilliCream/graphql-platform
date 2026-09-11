import type { Metadata } from "next";

import DepthStack from "../_prototypes/concepts/MissionControl/heroes/DepthStack";
import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v14 - Depth Stack",
  description:
    "Branch of the Mission Control prototype with a depth-stack hero: clients in front, the Fusion gateway in the middle and the subgraphs with their languages and federation specifications on the back plane.",
  robots: { index: false, follow: false },
};

export default function PrototypeV14Page() {
  return (
    <PrototypeShell version={14}>
      <MissionControl hero={<DepthStack />} />
    </PrototypeShell>
  );
}
