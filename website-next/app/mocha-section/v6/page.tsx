import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { MochaSectionV6 } from "@/src/components/home/mocha/MochaSectionV6";

export const metadata: Metadata = {
  title: "Mocha Section v6 · Every app runs on events",
  robots: { index: false, follow: false },
};

export default function MochaSectionV6Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <MochaSectionV6 />
      </div>
      <NitroPricing />
    </>
  );
}
