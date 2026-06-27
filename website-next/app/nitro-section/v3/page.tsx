import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { NitroSectionV3 } from "@/src/components/home/nitro/NitroSectionV3";

export const metadata: Metadata = {
  title: "Nitro Section v3",
  robots: { index: false, follow: false },
};

export default function NitroSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <NitroSectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
