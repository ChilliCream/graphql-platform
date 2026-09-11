import type { Metadata } from "next";

import QueryPlanTrace from "../_prototypes/concepts/MissionControl/heroes/QueryPlanTrace";
import { MissionControl } from "../_prototypes/concepts/MissionControl/MissionControl";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v13 - Query Plan Trace",
  description:
    "Prototype of the Fusion product page with the Mission Control layout and a query plan trace hero: a client's operation, the plan Fusion runs across five subgraphs and two non-GraphQL sources, and the fragments merging into one response.",
  robots: { index: false, follow: false },
};

export default function PrototypeV13Page() {
  return (
    <PrototypeShell version={13}>
      <MissionControl hero={<QueryPlanTrace />} />
    </PrototypeShell>
  );
}
