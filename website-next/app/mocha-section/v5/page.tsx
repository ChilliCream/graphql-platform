import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV5 } from "@/src/components/home/mocha/MochaSectionV5";

export const metadata: Metadata = {
  title: "Mocha Section v5 · At a glance",
  robots: { index: false, follow: false },
};

export default function MochaSectionV5Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV5 />
      </div>
      <NitroPricing />
    </>
  );
}
