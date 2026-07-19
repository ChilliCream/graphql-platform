import type { Metadata } from "next";

import { ExplainerPage } from "./ExplainerPage";

export const metadata: Metadata = {
  title: "What is GraphQL Federation?",
  description:
    "GraphQL federation combines the schemas of multiple independent services into one unified graph served at a single endpoint. Learn how composition, entities, and query planning work.",
  robots: { index: false, follow: false },
};

export default function FederationGraphicV5Page() {
  return <ExplainerPage />;
}
