import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro: Your API as a Living Graph",
  description:
    "Nitro is ChilliCream's control plane for GraphQL and .NET, set against a living constellation of nodes and edges: author operations, observe with OpenTelemetry, trace requests, diagnose errors, and evolve schemas safely.",
  robots: { index: false, follow: false },
};

export default function NitroPreviewV13Page() {
  return <ClientPage />;
}
