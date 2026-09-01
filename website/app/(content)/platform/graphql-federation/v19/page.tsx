import type { Metadata } from "next";

import { PiecesAndCards } from "../_prototypes/concepts/PiecesAndCards";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v19 - Pieces and Cards",
  description:
    "Backbone prototype: five service pieces and an evolving set of schema and query cards, staged as tabletop vignettes under one rule - pieces never stack, only cards do.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={19}>
      <PiecesAndCards />
    </PrototypeShell>
  );
}
