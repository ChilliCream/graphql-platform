import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV4 } from "@/src/components/home/mocha/MochaSectionV4";

export const metadata: Metadata = {
  title: "Mocha Section v4 · Capabilities",
  robots: { index: false, follow: false },
};

export default function MochaSectionV4Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV4 />
      </div>
      <NitroPricing />
    </>
  );
}
