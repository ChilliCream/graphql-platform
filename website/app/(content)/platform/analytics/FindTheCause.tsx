import type { ReactNode } from "react";

import dynamic from "next/dynamic";

import { ChapterBand } from "@/src/components/ChapterBand";
import { FeatureRow } from "@/src/components/FeatureRow";
import { MockWindowChrome } from "@/src/components/MockWindowChrome";

const NitroDiagnose = dynamic(() => import("@/src/nitro").then((m) => m.NitroDiagnose));
const NitroTrace = dynamic(() => import("@/src/nitro").then((m) => m.NitroTrace));

interface FramedVisualProps {
  readonly children: ReactNode;
}

function FramedVisual({ children }: FramedVisualProps) {
  return (
    <MockWindowChrome
      glow={{
        background: "radial-gradient(60% 60% at 50% 40%, rgba(94,234,212,0.16), transparent 70%)",
        inset: "-inset-x-6 -inset-y-4",
        blur: "blur-3xl",
        rounded: "rounded-[2rem]",
      }}
      shadow="none"
      rounded="rounded-xl"
      surfaceClassName="bg-cc-surface shadow-2xl shadow-black/40"
    >
      {children}
    </MockWindowChrome>
  );
}

export function FindTheCause() {
  return (
    <>
      <ChapterBand className="mt-12 sm:mt-16" title="Move from a metric spike to the related traces." />

      <section className="py-12 sm:py-16">
        <div className="flex flex-col gap-16 sm:gap-20">
          <FeatureRow
            title="Follow one request through its reported trace."
            body="Open the trace waterfall to inspect the services and spans that reported timing for one request, including which calls contributed the most latency."
            visual={
              <FramedVisual>
                <NitroTrace className="w-full" />
              </FramedVisual>
            }
          />

          <FeatureRow
            reverse
            title="Inspect the trace behind a failed operation."
            body="When errors spike, open the failing operation and inspect its traces, spans, and captured exception details without correlating logs by hand."
            visual={
              <FramedVisual>
                <NitroDiagnose className="w-full" />
              </FramedVisual>
            }
          />
        </div>
      </section>
    </>
  );
}
