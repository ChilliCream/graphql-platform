import type { Metadata } from "next";

import { EmberNetworkSection } from "@/src/components/home/mocha/messaging-graphic/EmberNetworkSection";

export const metadata: Metadata = {
  title: "Messaging Hero v6 (ember network)",
  robots: { index: false, follow: false },
};

export default function MessagingHeroV6Page() {
  return <EmberNetworkSection />;
}
