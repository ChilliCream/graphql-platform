import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { RunNumberRising } from "../_prototypes/concepts/RunNumberRising";

export const metadata: Metadata = {
  title: "Prototype v14 - Run Number Rising",
  description:
    "Federation problem-section prototype: Billing's CI pipeline card recurs at every beat with an incrementing run number, telling the whole story as one pipeline's biography.",
  robots: { index: false, follow: false },
};

export default function PrototypeV14Page() {
  return (
    <PrototypeShell version={14}>
      <RunNumberRising />
    </PrototypeShell>
  );
}
