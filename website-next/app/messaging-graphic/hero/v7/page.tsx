import type { Metadata } from "next";

import { MessagingWhySection } from "@/src/components/home/mocha/messaging-graphic/MessagingWhySection";

export const metadata: Metadata = {
  title: "Messaging Hero v7 (why, site style)",
  robots: { index: false, follow: false },
};

export default function MessagingHeroV7Page() {
  return <MessagingWhySection />;
}
