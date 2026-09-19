import type { Metadata } from "next";

import { SameScreen } from "../_prototypes/concepts/SameScreen";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v13 - The Same Screen",
  description:
    "Federation problem-section prototype: one recurring device-frame silhouette redrawn at every beat, with the port at its bottom edge standing in for what answers the screen today.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={13}>
      <SameScreen />
    </PrototypeShell>
  );
}
