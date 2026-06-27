import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedAgentic } from "@/src/components/home/combined/CombinedAgentic";

export const metadata: Metadata = {
  title: "Combined Section v3",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedAgentic />
      </div>
      <NitroPricing />
    </>
  );
}
