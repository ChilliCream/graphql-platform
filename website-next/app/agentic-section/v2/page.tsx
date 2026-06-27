import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { AgenticSectionV2 } from "@/src/components/home/agentic/AgenticSectionV2";

export const metadata: Metadata = {
  title: "Agentic Section v2 · Best practices your agent actually follows",
  robots: { index: false, follow: false },
};

export default function AgenticSectionV2Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <AgenticSectionV2 />
      </div>
      <NitroPricing />
    </>
  );
}
