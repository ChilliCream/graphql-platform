import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV7 } from "@/src/components/home/mocha/MochaSectionV7";

export const metadata: Metadata = {
  title: "Mocha Section v7 · Your app is mostly side effects",
  robots: { index: false, follow: false },
};

export default function MochaSectionV7Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV7 />
      </div>
      <NitroPricing />
    </>
  );
}
