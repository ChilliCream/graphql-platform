import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { NitroSectionV1 } from "@/src/components/home/nitro/NitroSectionV1";

export const metadata: Metadata = {
  title: "Nitro Section v1",
  robots: { index: false, follow: false },
};

export default function NitroSectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <NitroSectionV1 />
      </div>
      <NitroPricing />
    </>
  );
}
