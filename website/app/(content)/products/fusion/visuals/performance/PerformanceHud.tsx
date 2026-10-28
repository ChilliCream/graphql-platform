"use client";

import { useRef, type CSSProperties, type ReactNode } from "react";

import { NitroTheme } from "@/src/nitro";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { CenterOpsDial } from "./CenterOpsDial";
import { CLUSTER, RING_GLOW } from "./hud";
import { LatencyDial } from "./LatencyDial";
import { PressureDial } from "./PressureDial";
import { useTelemetryClock } from "./useTelemetryClock";

interface DialSlotProps {
  readonly dial: "latency" | "centre" | "pressure";
  readonly children: ReactNode;
}

function DialSlot({ dial, children }: DialSlotProps) {
  const row = CLUSTER.row[dial];
  const stack = CLUSTER.stack[dial];
  return (
    <div
      className="absolute top-(--stack-top) left-(--stack-left) w-(--stack-size) @min-[504px]/hud:top-(--row-top) @min-[504px]/hud:left-(--row-left) @min-[504px]/hud:w-(--row-size)"
      style={
        {
          "--stack-top": stack.top,
          "--stack-left": stack.left,
          "--stack-size": stack.size,
          "--row-top": row.top,
          "--row-left": row.left,
          "--row-size": row.size,
        } as CSSProperties
      }
    >
      {children}
    </div>
  );
}

const DIALS = ["latency", "pressure", "centre"] as const;

export function PerformanceHud() {
  const ref = useRef<HTMLDivElement>(null);
  const active = useElementMotion(ref);
  const reduced = useReducedMotionPreference();
  const telemetry = useTelemetryClock(active, reduced);

  return (
    <div ref={ref}>
      <NitroTheme theme="dark" style={{ background: "transparent" }}>
        <div className="@container/hud">
          <div
            className="relative aspect-(--stack-aspect) w-full @min-[504px]/hud:aspect-(--row-aspect)"
            style={
              {
                "--stack-aspect": CLUSTER.stack.aspect,
                "--row-aspect": CLUSTER.row.aspect,
              } as CSSProperties
            }
          >
            {DIALS.map((dial) => (
              <DialSlot key={`glow-${dial}`} dial={dial}>
                <div
                  aria-hidden="true"
                  className="aspect-square rounded-full"
                  style={{ boxShadow: RING_GLOW }}
                />
              </DialSlot>
            ))}
            <DialSlot dial="latency">
              <LatencyDial telemetry={telemetry} />
            </DialSlot>
            <DialSlot dial="pressure">
              <PressureDial telemetry={telemetry} />
            </DialSlot>
            <DialSlot dial="centre">
              <CenterOpsDial telemetry={telemetry} />
            </DialSlot>
          </div>
        </div>
      </NitroTheme>
    </div>
  );
}
