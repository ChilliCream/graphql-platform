import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { PlatformSectionV2 } from "@/src/components/home/platform/PlatformSectionV2";

export const metadata: Metadata = {
  title: "Platform Section v2 · The Loop",
  robots: { index: false, follow: false },
};

// Previewed in its real landing neighborhood: the section that precedes it
// (ProtocolCards), this take, then the pricing section it sits above.
export default function PlatformSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <PlatformSectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
