import type { Metadata } from "next";

import { DispatchBoard } from "../_prototypes/concepts/DispatchBoard";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v4 - The Dispatch Board",
  description:
    "Federation problem-section prototype: composition drawn as a routing-table panel spanning all five lanes, with a selective fan-out at the gateway.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={4}>
      <DispatchBoard />
    </PrototypeShell>
  );
}
