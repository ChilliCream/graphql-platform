import type { Metadata } from "next";

import { Editorial } from "../_prototypes/concepts/Editorial/Editorial";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v10 - Editorial",
  description:
    "Prototype of the Fusion product page as a hand-annotated magazine feature: marker diagrams that draw themselves, subgraphs on sticky notes, a red-pen correction for composition and a highlighter for Nitro.",
  robots: { index: false, follow: false },
};

export default function PrototypeV10Page() {
  return (
    <PrototypeShell version={10}>
      <Editorial />
    </PrototypeShell>
  );
}
