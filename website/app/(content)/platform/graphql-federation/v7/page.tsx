import type { Metadata } from "next";

import { DocumentProtagonist } from "../_prototypes/concepts/DocumentProtagonist";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v7 - The Document Is the Protagonist",
  description:
    "Federation problem-section prototype: a sticky mono schema panel whose field rows morph through REST calls, one query, source schemas, and composition, under an unchanging five-team footer.",
  robots: { index: false, follow: false },
};

export default function PrototypeV7Page() {
  return (
    <PrototypeShell version={7}>
      <DocumentProtagonist />
    </PrototypeShell>
  );
}
