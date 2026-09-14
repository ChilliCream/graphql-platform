import type { Metadata } from "next";

import LayeredDiagram from "../_prototypes/concepts/MissionControl/heroes/LayeredDiagram";
import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v11 - Layered Diagram",
  description:
    "Branch of the Mission Control prototype: the hero is a three-tier architecture diagram of clients, the Fusion gateway and the subgraphs and sources behind it.",
  robots: { index: false, follow: false },
};

export default function PrototypeV11Page() {
  return (
    <PrototypeShell version={11}>
      <MissionControl hero={<LayeredDiagram />} />
    </PrototypeShell>
  );
}
