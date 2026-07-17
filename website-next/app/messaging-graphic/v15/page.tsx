import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Messaging Graphic v15",
  robots: { index: false, follow: false },
};

export default function MessagingGraphicV15Page() {
  return <ClientPage />;
}
