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
          style={{ background: token.bg }}
        >
          <div className="flex flex-col gap-4 p-4 sm:p-5">
            <div className="flex flex-col items-center gap-5 @min-[560px]:flex-row @min-[560px]:items-center">
              <div className="flex flex-1 items-center justify-center">
                <Tachometer active={active} reduced={reduced} />
              </div>
              <div className="flex flex-col items-center gap-4 @min-[380px]:flex-row @min-[380px]:justify-center @min-[380px]:gap-6 @min-[560px]:w-[176px] @min-[560px]:shrink-0 @min-[560px]:flex-col @min-[560px]:gap-4">
                <LatencyGauge active={active} reduced={reduced} />
                <SideReadouts />
              </div>
            </div>
            <LiveThroughput active={active} />
            <StatusStrip />
          </div>
        </NitroTheme>
      </Card>
    </div>
  );
}
