import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "ChilliCream Services: Advisory, Support, Training",
  description:
    "Tell us where you are and the right ChilliCream GraphQL service comes to meet you: Advisory, Support plans from $450 per month, or Corporate Training.",
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
    title: "ChilliCream Services: Advisory, Support, Training",
    description:
      "Tell us where you are and the right ChilliCream GraphQL service comes to meet you: Advisory, Support, or Training, from the Hot Chocolate team.",
  },
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
