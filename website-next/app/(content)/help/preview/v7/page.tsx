import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Get GraphQL help fast",
  description:
    "GraphQL help routed by urgency. Pick a free Slack community, a $300 expert consultancy hour, or a tailored support plan. Watch a question find its tier.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "Get GraphQL help fast",
    description:
      "GraphQL help routed by urgency. Pick a free Slack community, a $300 expert consultancy hour, or a tailored support plan. Watch a question find its tier.",
  },
};

export default function HelpPreviewV7Page() {
  return <ClientPage />;
}
