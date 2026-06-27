import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { ObservabilitySectionV3 } from "@/src/components/home/observability/ObservabilitySectionV3";

export const metadata: Metadata = {
  title: "Observability Section v3",
  robots: { index: false, follow: false },
};

export default function ObservabilitySectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <ObservabilitySectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
