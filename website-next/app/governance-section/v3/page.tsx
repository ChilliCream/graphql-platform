import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { GovernanceSectionV3 } from "@/src/components/home/governance/GovernanceSectionV3";

export const metadata: Metadata = {
  title: "Governance Section v3",
  robots: { index: false, follow: false },
};

export default function GovernanceSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <GovernanceSectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
