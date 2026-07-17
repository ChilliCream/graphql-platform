import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Messaging Graphic v17",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV17Page() {
  return <ClientPage />;
}
