import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedGovernance } from "@/src/components/home/combined/CombinedGovernance";

export const metadata: Metadata = {
  title: "Combined Section v4",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV4Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedGovernance />
      </div>
      <NitroPricing />
    </>
  );
}
