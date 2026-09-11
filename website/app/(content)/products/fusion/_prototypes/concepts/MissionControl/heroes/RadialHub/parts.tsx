"use client";

import type { CSSProperties, ReactNode } from "react";

import { anim } from "../../hooks";
import { MC } from "../../palette";
import { C } from "./board";

/**
 * The two pieces the radial hub repeats: the light that travels a spoke and
 * the plate that sits at the end of one.
 */

interface LightProps {
  /** Spoke the light runs along. */
  readonly angle: number;
  /** Start and end of the run, in SVG user units from the centre. */
  readonly from: number;
  readonly to: number;
  readonly color: string;
  readonly delay: number;
  readonly duration: number;
  /** Fraction of the run the light parks at while the gate is closed, or `false` to hide it there. */
  readonly rest: number | false;
  readonly running: boolean;
}

/** A request or response travelling one spoke, drawn in the spoke's own frame. */
export function Light({
  angle,
  from,
  to,
  color,
  delay,
  duration,
  rest,
  running,
}: LightProps) {
  const distance = to - from;
  return (
    <g transform={`rotate(${angle} ${C} ${C})`}>
      <g
        style={
          {
            "--rh-d": `${distance.toFixed(1)}px`,
            animation: anim(
              running,
              `rh-run ${duration}ms ease-in-out ${delay}ms both`,
            ),
            transform:
              running || rest === false
                ? undefined
                : `translateX(${(distance * rest).toFixed(1)}px)`,
            opacity: running ? undefined : rest === false ? 0 : 0.9,
          } as CSSProperties
        }
      >
        <circle cx={from} cy={C} r="13" fill={color} fillOpacity="0.2" />
        <circle cx={from} cy={C} r="5" fill={color} />
      </g>
    </g>
  );
}

interface PlateProps {
  /** Ring placement, from `place`. */
  readonly style: CSSProperties;
  readonly children: ReactNode;
  /** Edge colour: the specification tint for a subgraph, the signal for a client. */
  readonly tone: string;
  /** True while this node takes part in the request in flight. */
  readonly active?: boolean;
}

export function Plate({ style, children, tone, active = false }: PlateProps) {
  return (
    <div
      className="absolute rounded-md border px-2 py-1 text-center"
      style={{
        ...style,
        background: MC.panel,
        borderColor: active
          ? `color-mix(in srgb, ${tone} 75%, transparent)`
          : `color-mix(in srgb, ${tone} 28%, ${MC.panelEdge})`,
        boxShadow: active
          ? `0 0 22px color-mix(in srgb, ${tone} 32%, transparent)`
          : "none",
        transition: "border-color 400ms, box-shadow 400ms",
        fontFamily: MC.mono,
        letterSpacing: "0.08em",
        textTransform: "uppercase",
        whiteSpace: "nowrap",
      }}
    >
      {children}
    </div>
  );
}
