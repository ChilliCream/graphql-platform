import type { Metadata } from "next";

import { Hero } from "./Hero";

export const metadata: Metadata = {
  title: "Federation Graphic v2",
  robots: { index: false, follow: false },
};

export default function FederationGraphicV2Page() {
  return <Hero />;
}
