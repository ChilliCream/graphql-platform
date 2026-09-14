import type { Metadata } from "next";

import { OneNight } from "../_prototypes/concepts/OneNight";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v18 - One Night",
  description:
    "Federation problem-section prototype: one continuous night sky, dusk to dawn, where the five services recur as clouds smeared into murk by the wrong answers and rim-lit by one dawn at composition.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return (
    <PrototypeShell version={18}>
      <OneNight />
    </PrototypeShell>
  );
}
