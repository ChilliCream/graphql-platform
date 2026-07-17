import type { Metadata } from "next";

import { MessagingGraphicV11 } from "@/src/components/home/mocha/messaging-graphic/MessagingGraphicV11";

export const metadata: Metadata = {
  title: "Messaging Graphic v11",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV11Page() {
  return <MessagingGraphicV11 />;
}
