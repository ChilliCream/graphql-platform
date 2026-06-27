import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV1 } from "@/src/components/home/mocha/MochaSectionV1";

export const metadata: Metadata = {
  title: "Mocha Section v1 · Sequence",
  robots: { index: false, follow: false },
};

export default function MochaSectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV1 />
      </div>
      <NitroPricing />
    </>
  );
}
