import type { Metadata } from "next";

import { NitroPricing } from "@/src/components/home/NitroPricing";
import { ProtocolCards } from "@/src/components/home/ProtocolCards";
import { AgenticSectionV1 } from "@/src/components/home/agentic/AgenticSectionV1";

export const metadata: Metadata = {
  title: "Agentic Section v1 · Keep the time your agent saves you",
  robots: { index: false, follow: false },
};

export default function AgenticSectionV1Page() {
  return (
    <>
      <ProtocolCards />
      <div id="take-section">
        <AgenticSectionV1 />
      </div>
      <NitroPricing />
    </>
  );
}
