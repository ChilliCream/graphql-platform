import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { CombinedNitro } from "@/src/components/home/combined/CombinedNitro";

export const metadata: Metadata = {
  title: "Combined Section v6",
  robots: { index: false, follow: false },
};

export default function CombinedSectionV6Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <CombinedNitro />
      </div>
      <NitroPricing />
    </>
  );
}
