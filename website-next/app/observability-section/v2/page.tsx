import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { ObservabilitySectionV2 } from "@/src/components/home/observability/ObservabilitySectionV2";

export const metadata: Metadata = {
  title: "Observability Section v2",
  robots: { index: false, follow: false },
};

export default function ObservabilitySectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <ObservabilitySectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
