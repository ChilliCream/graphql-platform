import type { Metadata } from "next";

import { MessagingGraphicV12 } from "@/src/components/home/mocha/messaging-graphic/MessagingGraphicV12";

export const metadata: Metadata = {
  title: "Messaging Graphic v12",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV12Page() {
  return <MessagingGraphicV12 />;
}
