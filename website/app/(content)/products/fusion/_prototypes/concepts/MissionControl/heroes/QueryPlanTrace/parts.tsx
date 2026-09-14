"use client";

import type { ReactNode } from "react";

import { TYPE } from "../../../../brand";
import { anim } from "../../hooks";
import { MC } from "../../palette";
import { TRACE_CLIENTS } from "./data";

/**
 * The plates, chips and connector rail the three trace columns are built from.
 * Every colour is a `MC` role and every size a `TYPE` step; the rail is plain
 * DOM so the lettering next to it never scales below its declared px size.
 */

/** Shared transition for everything that only changes with the plan step. */
const SETTLE =
  "color 400ms, border-color 400ms, background 400ms, opacity 400ms";

interface PlateProps {
  readonly title: string;
  readonly caption: string;
  readonly accent: string;
  readonly className?: string;
  /**
   * Extra classes for the header strip, so a plate whose title is not one of
   * the labels the hero has to show can drop it on a narrow screen.
   */
  readonly headerClassName?: string;
  readonly children: ReactNode;
}

/** One console plate: a titled surface the trace columns sit in. */
export function Plate({
  title,
  caption,
  accent,
  className,
  headerClassName,
  children,
}: PlateProps) {
  return (
    <div
      className={`flex flex-col rounded-lg border backdrop-blur-[2px] ${className ?? ""}`}
      style={{ background: MC.panel, borderColor: MC.panelEdge }}
    >
      <div
        className={`flex items-baseline justify-between gap-3 border-b px-3 py-2 ${headerClassName ?? ""}`}
        style={{ borderColor: MC.panelEdge }}
      >
        <span
          className="uppercase"
          style={{
            color: MC.ink,
            fontFamily: MC.mono,
            fontSize: TYPE.caption,
            letterSpacing: "0.2em",
          }}
        >
          {title}
        </span>
        <span
          className="uppercase"
          style={{
            color: accent,
            fontFamily: MC.mono,
            fontSize: TYPE.label,
            letterSpacing: "0.16em",
          }}
        >
          {caption}
        </span>
      </div>
      <div className="flex flex-1 flex-col px-3 py-3">{children}</div>
    </div>
  );
}

interface ChipProps {
  readonly label: string;
  readonly tone: string;
  readonly lit: boolean;
}

/** Language tag or specification badge next to a subgraph name. */
export function Chip({ label, tone, lit }: ChipProps) {
  return (
    <span
      className="rounded-sm border px-1.5 whitespace-nowrap"
      style={{
        color: lit ? tone : MC.dim,
        borderColor: `color-mix(in srgb, ${lit ? tone : MC.line} 55%, transparent)`,
        background: `color-mix(in srgb, ${tone} ${lit ? 12 : 0}%, transparent)`,
        fontFamily: MC.mono,
        fontSize: TYPE.label,
        letterSpacing: "0.08em",
        transition: SETTLE,
      }}
    >
      {label}
    </span>
  );
}

interface RailProps {
  /** The step this rail cell belongs to has been reached. */
  readonly lit: boolean;
  /** The gateway is fetching this step right now. */
  readonly active: boolean;
  readonly spineAbove: boolean;
  readonly spineBelow: boolean;
  readonly running: boolean;
}

/**
 * One cell of the connector rail: the spine the plan runs down, the elbow into
 * the row and, while the step is in flight, a signal travelling along it. The
 * three parallel rows share one unbroken spine, which is what makes the
 * fan-out and the merge readable as structure rather than decoration.
 */
export function Rail({
  lit,
  active,
  spineAbove,
  spineBelow,
  running,
}: RailProps) {
  const color = lit ? MC.phosphor : MC.line;

  return (
    <div className="relative w-5 shrink-0 self-stretch">
      {spineAbove && (
        <span
          className="absolute top-0 left-[9px] h-1/2 w-px"
          style={{ background: color, transition: SETTLE }}
        />
      )}
      {spineBelow && (
        <span
          className="absolute bottom-0 left-[9px] h-1/2 w-px"
          style={{ background: color, transition: SETTLE }}
        />
      )}
      <span
        className="absolute top-1/2 right-0 left-[9px] h-px"
        style={{ background: color, transition: SETTLE }}
      />
      <span
        className="absolute left-[6px] h-[7px] w-[7px] rounded-full border"
        style={{
          top: "calc(50% - 3.5px)",
          borderColor: color,
          background: lit ? color : MC.panel,
          transition: SETTLE,
        }}
      />
      {active && (
        <span
          className="absolute left-[9px] h-[5px] w-[5px] rounded-full"
          style={{
            top: "calc(50% - 2.5px)",
            background: MC.signal,
            animation: anim(running, "mc-qpt-flow 900ms ease-in-out infinite"),
            opacity: running ? undefined : 1,
          }}
        />
      )}
    </div>
  );
}

interface ClientTabsProps {
  /** The client whose operation the trace is currently running. */
  readonly activeIndex: number;
}

/** The four callers, one tab each; the live one drives the request plate. */
export function ClientTabs({ activeIndex }: ClientTabsProps) {
  return (
    <div className="flex flex-wrap gap-1.5">
      {TRACE_CLIENTS.map((tab, i) => {
        const live = i === activeIndex;
        return (
          <span
            key={tab.label}
            className="flex items-center gap-1.5 rounded-sm border px-2 py-1"
            style={{
              color: live ? MC.ink : MC.dim,
              borderColor: live
                ? `color-mix(in srgb, ${MC.signal} 60%, transparent)`
                : MC.panelEdge,
              background: live
                ? `color-mix(in srgb, ${MC.signal} 12%, transparent)`
                : MC.panel,
              fontFamily: MC.mono,
              fontSize: TYPE.label,
              letterSpacing: "0.14em",
              transition: SETTLE,
            }}
          >
            <span
              className="h-[6px] w-[6px] rounded-full"
              style={{
                background: live ? MC.signal : MC.line,
                transition: SETTLE,
              }}
            />
            {tab.label}
          </span>
        );
      })}
    </div>
  );
}
