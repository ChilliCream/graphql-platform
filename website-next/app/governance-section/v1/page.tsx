import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { GovernanceSectionV1 } from "@/src/components/home/governance/GovernanceSectionV1";

export const metadata: Metadata = {
  title: "Governance Section v1",
  robots: { index: false, follow: false },
};

export default function GovernanceSectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <GovernanceSectionV1 />
      </div>
      <NitroPricing />
    </>
  );
}
