"use client";

import { useRef } from "react";

import { Card } from "@/src/design-system/Card";
import { NitroTheme, token } from "@/src/nitro";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { CenterOpsDial } from "./CenterOpsDial";
import { LatencyDial } from "./LatencyDial";
import { PressureDial } from "./PressureDial";

export function PerformanceHud() {
  const ref = useRef<HTMLDivElement>(null);
  const active = useElementMotion(ref);
  const reduced = useReducedMotionPreference();

  return (
    <div ref={ref}>
      <Card className="overflow-hidden rounded-2xl" glow>
        <NitroTheme
          theme="dark"
          className="relative z-10"
          style={{
            background: `color-mix(in srgb, ${token.info} 25%, black)`,
            boxShadow: `inset 0 0 0 1px ${token.borderStrong}, inset 0 0 32px -8px ${token.info}`,
          }}
        >
          <div className="@container relative overflow-hidden p-4">
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-0"
              style={{
                background: `radial-gradient(closest-side, color-mix(in srgb, ${token.info} 62%, transparent), transparent 72%)`,
              }}
            />
            <div className="relative flex flex-wrap items-center justify-center gap-x-2 gap-y-6 @min-[640px]:flex-nowrap @min-[640px]:gap-x-0 @min-[640px]:gap-y-0">
              <div className="z-0 order-2 w-[46%] @min-[640px]:order-1 @min-[640px]:-mr-[6%] @min-[640px]:w-[34%]">
                <LatencyDial active={active} reduced={reduced} />
              </div>
              <div className="z-10 order-1 w-[68%] @min-[640px]:order-2 @min-[640px]:w-[46%]">
                <CenterOpsDial active={active} reduced={reduced} />
              </div>
              <div className="z-0 order-3 w-[46%] @min-[640px]:-ml-[6%] @min-[640px]:w-[34%]">
                <PressureDial active={active} reduced={reduced} />
              </div>
            </div>
          </div>
        </NitroTheme>
      </Card>
    </div>
  );
}
