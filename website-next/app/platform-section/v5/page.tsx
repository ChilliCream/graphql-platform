import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { PlatformSectionV5 } from "@/src/components/home/platform/PlatformSectionV5";

export const metadata: Metadata = {
  title: "Platform Section v5 · Index 01-05",
  robots: { index: false, follow: false },
};

// Previewed in its real landing neighborhood: the section that precedes it
// (ProtocolCards), this take, then the pricing section it sits above.
export default function PlatformSectionV5Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <PlatformSectionV5 />
      </div>
      <NitroPricing />
    </>
  );
}
