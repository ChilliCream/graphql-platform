import type { Metadata } from "next";

import { ThreeActMeasure } from "../_prototypes/concepts/ThreeActMeasure";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v17 - Three-Act Measure",
  description:
    "Federation problem-section prototype: a three-act essay whose column width narrates the story - wide chaos, a narrow schema, then full-width composition - punctuated by five plain-rule act-break slugs.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={17}>
      <ThreeActMeasure />
    </PrototypeShell>
  );
}
