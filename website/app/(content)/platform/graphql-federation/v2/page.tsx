import type { Metadata } from "next";

import { AssemblingTheDocument } from "../_prototypes/concepts/AssemblingTheDocument";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v2 - Assembling the Document",
  description:
    "Federation problem-section prototype: composition drawn as a schema document assembled row by row from each service's contribution.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={2}>
      <AssemblingTheDocument />
    </PrototypeShell>
  );
}
