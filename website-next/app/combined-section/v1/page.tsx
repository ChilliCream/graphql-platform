import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedPlatform } from "@/src/components/home/combined/CombinedPlatform";

export const metadata: Metadata = {
  title: "Combined Section v1",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedPlatform />
      </div>
      <NitroPricing />
    </>
  );
}
