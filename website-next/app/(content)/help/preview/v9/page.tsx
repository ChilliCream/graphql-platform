import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "GraphQL help, on the record",
  description:
    "GraphQL help logged like a console session: a free community of 7000+ practitioners, a $300 expert consultancy hour, or a tailored support plan with an SLA.",
  keywords: [
    "GraphQL help",
    "ChilliCream support",
    "Hot Chocolate consultancy",
    "GraphQL Slack community",
    "GraphQL support plan",
  ],
  robots: { index: false, follow: false },
  openGraph: {
    title: "GraphQL help, on the record",
    description:
      "GraphQL help logged like a console session: a free community of 7000+ practitioners, a $300 expert consultancy hour, or a tailored support plan with an SLA.",
  },
};

export default function HelpPreviewV9Page() {
  return <ClientPage />;
}
