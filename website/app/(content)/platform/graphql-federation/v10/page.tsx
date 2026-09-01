import type { Metadata } from "next";

import { RouteLedger } from "../_prototypes/concepts/RouteLedger";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v10 - Route Ledger",
  description:
    "Federation problem-section prototype: a single chapter rail with numbered badges and five-tick state chips whose arrangement, never their count, changes per beat.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={10}>
      <RouteLedger />
    </PrototypeShell>
  );
}
