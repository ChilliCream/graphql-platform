import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "ChilliCream GraphQL services: Advisory, Support, Training",
  description:
    "Animated switchboard for the ChilliCream GraphQL services hub: routes your team to Advisory, Support plans from $450 per month, or Corporate Training.",
  keywords: [
    "ChilliCream services",
    "GraphQL advisory",
    "GraphQL support plans",
    "GraphQL training",
    "Hot Chocolate consulting",
    "Fusion support",
    "Corporate GraphQL workshop",
  ],
  openGraph: {
    title: "ChilliCream GraphQL services: Advisory, Support, Training",
    description:
      "Animated switchboard for the ChilliCream GraphQL services hub: routes your team to Advisory, Support plans from $450 per month, or Corporate Training.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
