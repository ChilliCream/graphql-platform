import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { ConductorsScore } from "../_prototypes/concepts/ConductorsScore";

export const metadata: Metadata = {
  title: "Prototype v3 - Conductor's Score",
  description:
    "Prototype of the federation problem section: five never-bending service lines crossed once by a composite schema drawn as a score, with the gateway as conductor.",
  robots: { index: false, follow: false },
};

export default function PrototypeV3Page() {
  return (
    <PrototypeShell version={3}>
      <ConductorsScore />
    </PrototypeShell>
  );
}
