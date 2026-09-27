"use client";

import { useRef } from "react";

import { Card } from "@/src/design-system/Card";
import { NitroTheme, token } from "@/src/nitro";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { LatencyGauge } from "./LatencyGauge";
import { LiveThroughput } from "./LiveThroughput";
import { SideReadouts } from "./SideReadouts";
import { StatusStrip } from "./StatusStrip";
import { Tachometer } from "./Tachometer";

export function PerformanceHud() {
  const ref = useRef<HTMLDivElement>(null);
  const active = useElementMotion(ref);
  const reduced = useReducedMotionPreference();

  return (
    <div ref={ref}>
      <Card className="overflow-hidden rounded-2xl" glow>
        <NitroTheme
          theme="dark"
          className="@container relative z-10"
          style={{
            background: `color-mix(in srgb, ${token.bg} 88%, black)`,
            boxShadow: `inset 0 0 0 1px ${token.borderStrong}, inset 0 0 28px -8px ${token.accent}`,
          }}
        >
          <div className="flex flex-col gap-4 p-4 sm:p-5">
            <div className="flex flex-col items-center justify-center gap-5 @min-[560px]:flex-row @min-[560px]:items-end">
              <div className="w-full @min-[560px]:basis-[56%]">
                <Tachometer active={active} reduced={reduced} />
              </div>
              <div className="w-full @min-[560px]:basis-[42%]">
                <LatencyGauge active={active} reduced={reduced} />
              </div>
            </div>
            <SideReadouts />
            <LiveThroughput active={active} />
            <StatusStrip />
          </div>
        </NitroTheme>
      </Card>
    </div>
  );
}
