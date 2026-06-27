import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { NitroSectionV2 } from "@/src/components/home/nitro/NitroSectionV2";

export const metadata: Metadata = {
  title: "Nitro Section v2",
  robots: { index: false, follow: false },
};

export default function NitroSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <NitroSectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
