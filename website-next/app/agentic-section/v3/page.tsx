import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { AgenticSectionV3 } from "@/src/components/home/agentic/AgenticSectionV3";

export const metadata: Metadata = {
  title: "Agentic Section v3 · Built for any agent",
  robots: { index: false, follow: false },
};

export default function AgenticSectionV3Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <AgenticSectionV3 />
      </div>
      <NitroPricing />
    </>
  );
}
