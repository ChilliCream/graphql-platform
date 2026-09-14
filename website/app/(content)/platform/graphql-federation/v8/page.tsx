import type { Metadata } from "next";

import { InterstitialSigils } from "../_prototypes/concepts/InterstitialSigils";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v8 - Interstitial Sigils",
  description:
    "Prototype of the federation problem section: seven small evolving divider glyphs, one above each chapter, built from a strict two-token vocabulary of service squares and schema braces.",
  robots: { index: false, follow: false },
};

export default function PrototypeV8Page() {
  return (
    <PrototypeShell version={8}>
      <InterstitialSigils />
    </PrototypeShell>
  );
}
