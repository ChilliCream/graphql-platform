import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { KeyedTiles } from "../_prototypes/concepts/KeyedTiles";

export const metadata: Metadata = {
  title: "Prototype v9 - Keyed Tiles",
  description:
    "Prototype of the federation problem section: each team's schema is a notched tile, and composition clicks the tiles into one panel whose seams stay visible.",
  robots: { index: false, follow: false },
};

export default function PrototypeV9Page() {
  return (
    <PrototypeShell version={9}>
      <KeyedTiles />
    </PrototypeShell>
  );
}
