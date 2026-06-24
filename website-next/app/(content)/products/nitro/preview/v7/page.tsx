import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

// Single-route v7 entry. The Next 16 App Router only honors `export const metadata`
// from a Server Component, while the v7 motion-showcase body needs hooks
// (useInView, useReducedMotion, useMotionValue, useTransform, animate,
// AnimatePresence), so the client subtree lives in ClientPage. The page module
// itself stays a thin Server Component shell so the metadata below is emitted
// into the initial HTML.

export const metadata: Metadata = {
  title: "Nitro: The Live Cockpit for GraphQL APIs",
  description:
    "The Nitro GraphQL control plane from ChilliCream: a live cockpit that traces requests, diagnoses errors, and evolves your schema without breaking clients.",
  robots: { index: false, follow: false },
};

export default function Page() {
  return <ClientPage />;
}
