import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV8 } from "@/src/components/home/mocha/MochaSectionV8";

export const metadata: Metadata = {
  title: "Mocha Section v8 · Simple, and it scales",
  robots: { index: false, follow: false },
};

export default function MochaSectionV8Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV8 />
      </div>
      <NitroPricing />
    </>
  );
}
