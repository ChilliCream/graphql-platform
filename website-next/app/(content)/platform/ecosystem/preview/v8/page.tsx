import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Departures Board: The GraphQL IDE for Teams",
  description:
    "Nitro GraphQL IDE as a departures board. Boarding-pass cards for OAuth, workspaces, document sync, PWA install, themes, multipart uploads, macOS, Windows.",
  keywords: [
    "Nitro GraphQL IDE",
    "GraphQL IDE Nitro",
    "Banana Cake Pop",
    "GraphQL workspace",
    "GraphQL OAuth 2",
    "GraphQL document sync",
    "GraphQL file upload multipart",
    "GraphQL PWA",
    "cross platform GraphQL IDE",
    "GraphQL collaboration",
  ],
  openGraph: {
    title: "Nitro Departures Board",
    description:
      "Every Nitro capability as a boarding pass. Pick a workspace, pass through OAuth, sync your documents, and land on any platform with the same IDE.",
  },
  robots: { index: false, follow: false },
};

export default function EcosystemV8Page() {
  return <ClientPage />;
}
