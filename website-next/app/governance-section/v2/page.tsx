import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { GovernanceSectionV2 } from "@/src/components/home/governance/GovernanceSectionV2";

export const metadata: Metadata = {
  title: "Governance Section v2",
  robots: { index: false, follow: false },
};

export default function GovernanceSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <GovernanceSectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
