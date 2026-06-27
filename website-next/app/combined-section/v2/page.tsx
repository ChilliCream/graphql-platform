import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedMocha } from "@/src/components/home/combined/CombinedMocha";

export const metadata: Metadata = {
  title: "Combined Section v2",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedMocha />
      </div>
      <NitroPricing />
    </>
  );
}
