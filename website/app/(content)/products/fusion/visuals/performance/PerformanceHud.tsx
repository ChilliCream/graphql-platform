"use client";

import { useRef, type CSSProperties, type ReactNode } from "react";

import { Card } from "@/src/design-system/Card";
import { NitroTheme, token } from "@/src/nitro";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { CenterOpsDial } from "./CenterOpsDial";
import { CLUSTER, ELECTRIC } from "./hud";
import { LatencyDial } from "./LatencyDial";
import { PressureDial } from "./PressureDial";

interface DialSlotProps {
  readonly slot: (typeof CLUSTER)["centre"];
  readonly children: ReactNode;
}

function DialSlot({ slot, children }: DialSlotProps) {
  return (
    <div
      className="absolute top-(--stack-top) left-(--stack-left) w-(--stack-size) @min-[416px]:top-(--row-top) @min-[416px]:left-(--row-left) @min-[416px]:w-(--row-size)"
      style={
        {
          "--stack-top": slot.stack.top,
          "--stack-left": slot.stack.left,
          "--stack-size": slot.stack.size,
          "--row-top": slot.row.top,
          "--row-left": slot.row.left,
          "--row-size": slot.row.size,
        } as CSSProperties
      }
    >
      {children}
    </div>
  );
}

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
            background: `color-mix(in srgb, ${ELECTRIC} 20%, black)`,
            boxShadow: `inset 0 0 0 1px ${token.borderStrong}, inset 0 0 40px -8px ${ELECTRIC}`,
          }}
        >
          <div className="@container relative overflow-hidden p-4">
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-0"
              style={{
                background: `radial-gradient(closest-side, color-mix(in srgb, ${ELECTRIC} 55%, transparent), transparent 88%)`,
              }}
            />
            <div
              className="relative aspect-(--stack-aspect) w-full @min-[416px]:aspect-(--row-aspect)"
              style={
                {
                  "--stack-aspect": CLUSTER.stackAspect,
                  "--row-aspect": CLUSTER.rowAspect,
                } as CSSProperties
              }
            >
              <DialSlot slot={CLUSTER.latency}>
                <LatencyDial active={active} reduced={reduced} />
              </DialSlot>
              <DialSlot slot={CLUSTER.centre}>
                <CenterOpsDial active={active} reduced={reduced} />
              </DialSlot>
              <DialSlot slot={CLUSTER.pressure}>
                <PressureDial active={active} reduced={reduced} />
              </DialSlot>
            </div>
          </div>
        </NitroTheme>
      </Card>
    </div>
  );
}
