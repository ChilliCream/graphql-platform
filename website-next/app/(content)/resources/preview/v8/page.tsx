import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Resource Ticker | ChilliCream Resources",
  description:
    "The ChilliCream resources hub reframed as a live broadcast strip: docs, blog, videos, Slack, GitHub, contact, shop, and legal, read off one live ticker.",
  keywords: [
    "ChilliCream resources",
    "GraphQL docs",
    "Hot Chocolate",
    "Nitro",
    "ChilliCream community",
    "ChilliCream legal",
  ],
  openGraph: {
    title: "Resource Ticker | ChilliCream Resources",
    description:
      "A broadcast-strip resources hub: docs, blog, videos, Slack, GitHub, contact, shop, and legal, running across a live ticker.",
  },
  robots: { index: false, follow: false },
};

export default function ResourcesTickerPage() {
  return <ClientPage />;
}
