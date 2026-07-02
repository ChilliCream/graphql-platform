import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro: Control Plane for GraphQL APIs",
  description:
    "Nitro is ChilliCream's control plane for GraphQL and .NET, set on an ambient dot-matrix field where a slow teal glow drifts across and brightens the lattice beneath it.",
  robots: { index: false, follow: false },
};

export default function NitroPreviewV11Page() {
  return <ClientPage />;
}
