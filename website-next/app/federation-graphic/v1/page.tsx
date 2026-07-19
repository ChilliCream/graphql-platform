import type { Metadata } from "next";

import { Hero } from "./Hero";

export const metadata: Metadata = {
  title: "Federation Graphic v1",
  robots: { index: false, follow: false },
};

export default function FederationGraphicV1Page() {
  return <Hero />;
}
