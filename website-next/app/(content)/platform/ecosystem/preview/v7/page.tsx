import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Mesh in Motion: The GraphQL IDE Nitro for Teams",
  description:
    "GraphQL IDE Nitro animates the cross-device workspace story end to end: OAuth, document sync, PWA install, themes, and multipart uploads, shown in motion.",
  keywords: [
    "GraphQL IDE Nitro",
    "Nitro GraphQL IDE",
    "Banana Cake Pop",
    "GraphQL workspace",
    "GraphQL OAuth 2",
    "GraphQL document sync",
    "GraphQL file upload multipart",
    "GraphQL PWA",
    "cross platform GraphQL IDE",
  ],
  openGraph: {
    title: "Nitro Mesh in Motion",
    description:
      "One GraphQL document, animated traveling from a workspace through OAuth, sync rails, and PWA surfaces, so the cross-device IDE story is felt before it is read.",
  },
  robots: { index: false, follow: false },
};

export default function EcosystemV7Page() {
  return <ClientPage />;
}
