import type { Metadata } from "next";

import { FiveLamps } from "../_prototypes/concepts/FiveLamps";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v20 - Five Lamps",
  description:
    "Federation problem-section prototype: five service-colored lamp glows hold fixed peripheral positions for the whole scroll, telling the story through what light falls on each beat's card.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={20}>
      <FiveLamps />
    </PrototypeShell>
  );
}
