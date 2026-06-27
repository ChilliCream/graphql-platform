import type { Metadata } from "next";

import { LandingCustomizer } from "@/src/components/home/customizer/LandingCustomizer";

export const metadata: Metadata = {
  title: "Landing Customizer",
  description:
    "Assemble a candidate landing from the section takes: enable, disable, and reorder.",
  robots: { index: false, follow: false },
};

export default function LandingCustomizerPage() {
  return <LandingCustomizer />;
}
