import type { Metadata } from "next";

import { BadgeP42 } from "../_prototypes/concepts/BadgeP42";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v16 - Badge P-42",
  description:
    "Federation problem-section prototype: a 64x24 product-id badge redrawn identically at every beat, joined by sameness rather than convergence.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={16}>
      <BadgeP42 />
    </PrototypeShell>
  );
}
