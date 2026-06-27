import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedObservability } from "@/src/components/home/combined/CombinedObservability";

export const metadata: Metadata = {
  title: "Combined Section v5",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV5Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedObservability />
      </div>
      <NitroPricing />
    </>
  );
}
