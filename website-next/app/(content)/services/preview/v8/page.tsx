import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "ChilliCream GraphQL Services: Advisory, Support, Training",
  description:
    "Three doors for the ChilliCream GraphQL services hub: float into Advisory, ongoing Support plans from $450 per month, or Corporate Training for your team.",
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
    title: "ChilliCream GraphQL Services: Advisory, Support, Training",
    description:
      "Three doors for the ChilliCream GraphQL services hub: float into Advisory, ongoing Support plans from $450 per month, or Corporate Training for your team.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
