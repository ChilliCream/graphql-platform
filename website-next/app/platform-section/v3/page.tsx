import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { PlatformSectionV3 } from "@/src/components/home/platform/PlatformSectionV3";

export const metadata: Metadata = {
  title: "Platform Section v3 · Bento",
  robots: { index: false, follow: false },
};

// Previewed in its real landing neighborhood: the section that precedes it
// (ProtocolCards), this take, then the pricing section it sits above.
export default function PlatformSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <PlatformSectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
