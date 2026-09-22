import type { Metadata } from "next";

import { PrototypeShell } from "../_prototypes/PrototypeShell";
import { FolioNumerals } from "../_prototypes/concepts/FolioNumerals";

export const metadata: Metadata = {
  title: "Prototype v11 - Folio Numerals",
  description:
    "Federation problem-section prototype: seven chapter plates fronted by oversized outline numerals, each captioned with who merges the data.",
  robots: { index: false, follow: false },
};

export default function PrototypeV11Page() {
  return (
    <PrototypeShell version={11}>
      <FolioNumerals />
    </PrototypeShell>
  );
}
