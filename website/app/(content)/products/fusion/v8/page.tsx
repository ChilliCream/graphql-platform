import type { Metadata } from "next";

import { Terminal } from "../_prototypes/concepts/Terminal/Terminal";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v8 - Terminal",
  description:
    "Prototype of the Fusion product page as a terminal session: a live composition run, a gateway log tail, a two-specification diff, a failing pipeline and the Nitro operation registry, all in monospace text.",
  robots: { index: false, follow: false },
};

export default function PrototypeV8Page() {
  return (
    <PrototypeShell version={8}>
      <Terminal />
    </PrototypeShell>
  );
}
