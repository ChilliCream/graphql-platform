import type { Metadata } from "next";

import { SceneSlates } from "../_prototypes/concepts/SceneSlates";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v15 - Scene Slates",
  description:
    "Federation problem-section prototype: seven storyboard frames sharing one production-slate header, whose ON SET row of five service squares carries the argument beat to beat.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={15}>
      <SceneSlates />
    </PrototypeShell>
  );
}
