import type { Metadata } from "next";

import { MessagingGraphicV9 } from "@/src/components/home/mocha/messaging-graphic/MessagingGraphicV9";

export const metadata: Metadata = {
  title: "Messaging Graphic v9",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV9Page() {
  return <MessagingGraphicV9 />;
}
