import type { Metadata } from "next";

import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v1 - Mission Control",
  description:
    "Prototype of the Fusion product page as a mission control room: a wall map with a radar sweep, tracked query blips, a composition pre-flight checklist and the Nitro flight recorder.",
  robots: { index: false, follow: false },
};

export default function PrototypeV1Page() {
  return (
    <PrototypeShell version={1}>
      <MissionControl />
    </PrototypeShell>
  );
}
