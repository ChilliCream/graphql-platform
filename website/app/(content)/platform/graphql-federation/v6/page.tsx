import type { Metadata } from "next";

import { AuthorshipColumn } from "../_prototypes/concepts/AuthorshipColumn";
import { PrototypeShell } from "../_prototypes/PrototypeShell";

export const metadata: Metadata = {
  title: "Prototype v6 - Authorship Column",
  description:
    "Prototype of the federation problem section: the composite schema as a git-blame-style document with a colored gutter per field, fed by five persistent file chips and a typeset runtime call sheet.",
  robots: { index: false, follow: false },
};

export default function PrototypeV6Page() {
  return (
    <PrototypeShell version={6}>
      <AuthorshipColumn />
    </PrototypeShell>
  );
}
