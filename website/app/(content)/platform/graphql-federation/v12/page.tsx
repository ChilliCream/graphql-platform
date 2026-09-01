import type { Metadata } from "next";

import { FrontMatter } from "../_prototypes/concepts/FrontMatter";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v12 - Front Matter",
  description:
    "Prototype of the federation problem section: a table-of-contents card and a repeating running head carry an APIS · TEAMS tally that converges to 1 · 5, told entirely in typography.",
  robots: { index: false, follow: false },
};

export default function PrototypeV12Page() {
  return (
    <PrototypeShell version={12}>
      <FrontMatter />
    </PrototypeShell>
  );
}
