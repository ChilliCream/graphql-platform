import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro: The Control Plane for GraphQL",
  description:
    "Nitro is ChilliCream's control plane for GraphQL and .NET: author operations, observe with OpenTelemetry, trace and diagnose requests, evolve schemas safely, and compose federated graphs with Fusion.",
  robots: { index: false, follow: false },
};

export default function NitroPreviewV16Page() {
  return <ClientPage />;
}
