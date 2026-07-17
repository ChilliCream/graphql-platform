import type { Metadata } from "next";

import { HeroBoard } from "@/src/components/home/mocha/messaging-graphic/HeroBoard";

export const metadata: Metadata = {
  title: "Messaging Hero v1 (cascade)",
  robots: { index: false, follow: false },
};

export default function MessagingHeroV1Page() {
  return <HeroBoard mode="cascade" />;
}
