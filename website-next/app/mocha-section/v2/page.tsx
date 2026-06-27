import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV2 } from "@/src/components/home/mocha/MochaSectionV2";

export const metadata: Metadata = {
  title: "Mocha Section v2 · In-process / across services",
  robots: { index: false, follow: false },
};

export default function MochaSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
