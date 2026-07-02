import type { Metadata } from "next";

import { ClientPage } from "./ClientPage";

export const metadata: Metadata = {
  title: "Nitro Preview - Drifting Line Grid",
  description:
    "Nitro's control plane over an animated graph-paper line grid: a clearly visible ivory grid that slowly drifts and fades from the hero, with a slow diagonal teal light sweep.",
  robots: { index: false, follow: false },
};

export default function NitroPreviewV12Page() {
  return <ClientPage />;
}
