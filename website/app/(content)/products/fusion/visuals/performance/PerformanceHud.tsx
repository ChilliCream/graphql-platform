"use client";

import { useRef, type CSSProperties, type ReactNode } from "react";

import { Card } from "@/src/design-system/Card";
import { NitroTheme, token } from "@/src/nitro";
import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import { useElementMotion } from "../hooks";
import { CenterOpsDial } from "./CenterOpsDial";
import {
  BACKDROP,
  BACKDROP_GLOW,
  CLUSTER,
  ELECTRIC_DIM,
  RING_GLOW,
} from "./hud";
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
      className="absolute top-(--stack-top) left-(--stack-left) w-(--stack-size) @min-[464px]/hud:top-(--row-top) @min-[464px]/hud:left-(--row-left) @min-[464px]/hud:w-(--row-size)"
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
      <Card className="overflow-hidden rounded-2xl" glow>
        <NitroTheme
          theme="dark"
          className="relative z-10"
          style={{
            background: BACKDROP,
            boxShadow: `inset 0 0 0 1px ${token.borderStrong}, inset 0 0 40px -8px ${ELECTRIC_DIM}`,
          }}
        >
          <div className="@container/hud relative overflow-hidden p-4">
            <div
              aria-hidden="true"
              className="pointer-events-none absolute inset-0"
              style={{
                background: `radial-gradient(closest-side, ${BACKDROP_GLOW}, transparent)`,
              }}
            />
            <div
              className="relative aspect-(--stack-aspect) w-full @min-[464px]/hud:aspect-(--row-aspect)"
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
      </Card>
    </div>
  );
}
