import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV3 } from "@/src/components/home/mocha/MochaSectionV3";

export const metadata: Metadata = {
  title: "Mocha Section v3 · One message, end to end",
  robots: { index: false, follow: false },
};

export default function MochaSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
