import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro IDE: Receipt for a GraphQL Workspace",
  description:
    "Provision a Nitro GraphQL IDE workspace, printed as a thermal receipt: OAuth sign in, shared workspaces, document sync, PWA install on macOS, Windows, Linux.",
  keywords: [
    "Nitro GraphQL IDE",
    "Banana Cake Pop",
    "GraphQL workspace",
    "GraphQL OAuth 2",
    "GraphQL document sync",
    "GraphQL file upload multipart",
    "GraphQL PWA",
    "GraphQL IDE PWA",
    "cross platform GraphQL IDE",
    "GraphQL collaboration",
  ],
  openGraph: {
    title: "Nitro: Receipt for Your GraphQL Workspace",
    description:
      "Author operations, share workspaces, and sync documents in the browser or as a PWA on macOS, Windows, and Linux. The GraphQL IDE your team actually opens.",
  },
  robots: { index: false, follow: false },
};

export default function EcosystemV9Page() {
  return <ClientPage />;
}
