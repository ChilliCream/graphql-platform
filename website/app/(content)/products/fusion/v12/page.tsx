import type { Metadata } from "next";

import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import RadialHub from "../_prototypes/concepts/MissionControl/heroes/RadialHub";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v12 - Radial Hub",
  description:
    "Branch of the Mission Control prototype with a radial hero: the Fusion gateway at the centre of a ring of subgraphs with their languages and federation specifications, two non-GraphQL sources and an outer ring of clients.",
  robots: { index: false, follow: false },
};

export default function PrototypeV12Page() {
  return (
    <PrototypeShell version={12}>
      <MissionControl hero={<RadialHub />} />
    </PrototypeShell>
  );
}
