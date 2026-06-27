import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { ObservabilitySectionV1 } from "@/src/components/home/observability/ObservabilitySectionV1";

export const metadata: Metadata = {
  title: "Observability Section v1",
  robots: { index: false, follow: false },
};

export default function ObservabilitySectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <ObservabilitySectionV1 />
      </div>
      <NitroPricing />
    </>
  );
}
