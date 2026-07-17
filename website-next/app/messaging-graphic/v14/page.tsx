import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Messaging Graphic v14",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV14Page() {
  return <ClientPage />;
}
