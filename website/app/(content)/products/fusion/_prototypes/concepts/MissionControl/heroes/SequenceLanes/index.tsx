"use client";

import { useRef } from "react";

import { Eyebrow } from "@/src/design-system/Eyebrow";

import { anim, useCycle, useElementMotion } from "../../hooks";
import { MC } from "../../palette";
import {
  GATEWAY,
  KEYFRAMES,
  LANES,
  NOW_COL,
  REST_STEP,
  SCROLL_MS,
  SEQ_MS,
  SEQUENCES,
  laneTop,
  wash,
} from "./lanes";
import { LanePlate, SequenceBlock } from "./parts";

/**
 * Hero visual: the request as a sequence diagram.
 *
 * One lifeline per participant, stacked top to bottom - the four clients (web
 * app, mobile app, partner API, AI agent), the Fusion gateway on the
 * emphasised lane, the five subgraphs (Catalog, Billing, Ordering, Shipping,
 * Accounts) with their language and federation spec, and the two non-GraphQL
 * sources (OpenAPI, gRPC). Time runs left to right: a request drops from a
 * client lane to the gateway, the gateway fans arrows out to the source
 * schemas it needs - all starting at the same x, so the fan-out reads as
 * parallel - the dashed returns come back to the gateway lane one after
 * another, and one merged response arrow goes back up to the client. The
 * strip carries the four columns twice and scrolls one full set leftwards per
 * sequence, the way a trace viewer does. Each hop draws itself at the moment
 * its own place on the strip crosses the now-line - one column in from the
 * left edge of the diagram box, which is the box's right edge at base and 75%
 * / 60% of the way across from `sm` / `lg` up - so the strip right of that
 * line is the not-yet-happened future and stays blank, and the wrap lands on
 * the identical twin column at the identical phase: the loop is seamless.
 *
 * Sizing: the diagram is DOM text on a fixed lane pitch, not a scaled SVG, so
 * every label renders at its own px size (11px floor) at any viewport width;
 * the lanes are pushed right of the hero copy from `lg` up. On a viewport too
 * short for twelve lanes the outer lanes crop, the way the wall map crops its
 * edges, rather than the lettering shrinking below the floor.
 *
 * Rest frame: with motion off the scroll parks on the first column and every
 * arrow and activation bar sits at its finished state, so the still picture
 * is the web app's one completed sequence - request, fan-out, returns, merged
 * response - filling the box, with the leading edge of the next one beside it
 * from `sm` up.
 */

export default function SequenceLanes() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const step = useCycle(running, SEQUENCES.length, SCROLL_MS, REST_STEP);
  // The column crossing the now-line during this step, not the one entering
  // the strip: that is the sequence the panel and the client plate name.
  const inFlight = SEQUENCES[(step + NOW_COL) % SEQUENCES.length];

  return (
    <div
      ref={ref}
      aria-hidden="true"
      className="absolute inset-0 overflow-hidden font-mono [--mc-seq-lane:48px] sm:[--mc-seq-lane:54px] lg:[--mc-seq-lane:56px]"
    >
      <style>{KEYFRAMES}</style>
      <div
        className="absolute inset-0"
        style={{
          backgroundImage: `repeating-linear-gradient(to right, ${MC.grid} 0 1px, transparent 1px 72px)`,
        }}
      />

      <div className="absolute inset-0 flex items-center">
        <div className="flex w-full items-stretch gap-2 px-3 sm:px-5 lg:pr-10 lg:pl-[58%] xl:pl-[50%]">
          <div className="flex w-[136px] shrink-0 flex-col sm:w-[168px] lg:w-[184px]">
            {LANES.map((lane, i) => (
              <LanePlate
                key={lane.name}
                lane={lane}
                active={i === inFlight.client}
              />
            ))}
          </div>

          <div className="relative min-w-0 flex-1 overflow-hidden">
            {LANES.map((lane, i) => (
              <div
                key={lane.name}
                className="absolute right-0 left-0 h-px"
                style={{
                  top: laneTop(i),
                  background:
                    i === GATEWAY
                      ? wash(MC.phosphor, 45)
                      : `repeating-linear-gradient(to right, ${MC.line} 0 4px, transparent 4px 10px)`,
                }}
              />
            ))}

            <div
              className="absolute top-0 bottom-0 left-0 flex w-[800%] sm:w-[600%] lg:w-[480%]"
              style={{
                animation: anim(
                  running,
                  `mc-seq-scroll ${SEQ_MS}ms linear infinite`,
                ),
              }}
            >
              {[...SEQUENCES, ...SEQUENCES].map((sequence, i) => (
                <SequenceBlock
                  key={i}
                  sequence={sequence}
                  delayMs={(i % SEQUENCES.length) * SCROLL_MS - SEQ_MS}
                  running={running}
                />
              ))}
            </div>
          </div>
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg absolute right-4 bottom-4 rounded-md border px-3 py-2 sm:right-8 sm:bottom-8">
        <Eyebrow color="ink-dim" size="2xs">
          IN FLIGHT
        </Eyebrow>
        <p className="text-h6 font-mono" style={{ color: MC.signal }}>
          {LANES[inFlight.client].name}
        </p>
        <p className="font-mono text-[11px]" style={{ color: MC.dim }}>
          {`Fusion → ${inFlight.targets.length} source schemas`}
        </p>
      </div>
    </div>
  );
}
