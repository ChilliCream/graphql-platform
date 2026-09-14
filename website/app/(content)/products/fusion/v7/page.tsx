import type { Metadata } from "next";

import { Blueprint } from "../_prototypes/concepts/Blueprint/Blueprint";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v7 - Blueprint",
  description:
    "Prototype of the Fusion product page as an engineering drawing set: an exploded general arrangement of the gateway, a query dimensioned across sub-assemblies, specification stamps, a tolerance check that red-lines a conflict and Nitro as the as-built record.",
  robots: { index: false, follow: false },
};

export default function PrototypeV7Page() {
  return (
    <PrototypeShell version={7}>
      <Blueprint />
    </PrototypeShell>
  );
}
