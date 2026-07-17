import type { Metadata } from "next";

import { MessagingGraphicV10 } from "@/src/components/home/mocha/messaging-graphic/MessagingGraphicV10";

export const metadata: Metadata = {
  title: "Messaging Graphic v10",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV10Page() {
  return <MessagingGraphicV10 />;
}
