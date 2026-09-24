"use client";

import { useRef } from "react";

import { anim, useCycle, useElementMotion } from "./hooks";
import { MC, specTag } from "./palette";
import type { StationSpec } from "./palette";
import { BRAND, TYPE } from "./tokens";
import {
  BUS_Y,
  PHASE_LABEL,
  PHASE_MS,
  SPEC_LEGEND,
  columnLanes,
  restStep,
} from "./diagram";
import type { BandFlow, ClientNode, Request, TierNode } from "./diagram";
import { Card, Elbow, LINE_HEIGHT, Pulse, wash } from "./parts";

/**
 * Diagram visual: the architecture drawn as a three-tier diagram, with a
 * client card per `clients` entry, one gateway panel, and one card per
 * `tiers` entry along the bottom. Each request fans out from a client
 * through the gateway to the sources it needs and merges back into one
 * response. Renders directly inside a panel box of whatever width its
 * caller gives it (a half-width feature-row column, say); the tiers collapse
 * from 4 and 7 columns to 2 and 3 with a CSS container query on the
 * diagram's own width, not the viewport's, so the layout is right from the
 * first paint with no measured state.
 */

interface LayeredDiagramProps {
  /** Name on the gateway panel, e.g. "Fusion". */
  readonly gatewayLabel: string;
  /** Readout line under the gateway's phase indicator. */
  readonly compositionLine: string;
  readonly clients: readonly ClientNode[];
  readonly tiers: readonly TierNode[];
  /** Request script the diagram replays; every caller passes its own. */
  readonly requests: readonly Request[];
}

/** Column counts per tier, stacked (below the container query) and wide. */
const CLIENT_COLUMNS = [2, 4] as const;
const NODE_COLUMNS = [3, 7] as const;

const LAYOUTS = [
  { key: "stacked", className: "@min-[760px]:hidden" },
  { key: "wide", className: "hidden @min-[760px]:block" },
] as const;

const KEYFRAMES = `
@keyframes mc-layer-in {
  0% { top: 0%; left: var(--mc-layer-x); opacity: 0; }
  10% { opacity: 1; }
  50% { top: ${BUS_Y["to-gateway"]}%; left: var(--mc-layer-x); }
  78% { top: ${BUS_Y["to-gateway"]}%; left: 50%; }
  100% { top: 100%; left: 50%; opacity: 1; }
}
@keyframes mc-layer-out {
  0% { top: 0%; left: 50%; opacity: 1; }
  22% { top: ${BUS_Y["from-gateway"]}%; left: 50%; }
  50% { top: ${BUS_Y["from-gateway"]}%; left: var(--mc-layer-x); }
  92% { opacity: 1; }
  100% { top: 100%; left: var(--mc-layer-x); opacity: 0; }
}
`;

const HUB_GLOW = `radial-gradient(48% 36% at 50% 50%, ${wash(MC.phosphor, 16)} 0%, transparent 72%)`;

/** Teal for the GraphQL specification, violet for Apollo's, dim for a source. */
function specColor(spec: StationSpec | null): string {
  if (spec === null) return MC.dim;
  return spec === "Apollo Federation" ? BRAND.violet : MC.phosphor;
}

interface BandProps {
  readonly flow: BandFlow;
  /** Cards in the tier this band joins to the gateway. */
  readonly count: number;
  /** Columns that tier uses, stacked and wide. */
  readonly columns: readonly [number, number];
  /** Indexes of the lit cards; their columns carry the traffic. */
  readonly lit: readonly number[];
  readonly tone: string;
  /** Remounts the pulses so each step restarts its animation. */
  readonly step: number;
  /** Animation for the `index`-th lit lane, or `null` when nothing travels. */
  readonly pulse: ((index: number) => string) | null;
}

/**
 * The connectors between one tier and the gateway: a rounded elbow per
 * column down to a shared horizontal run, and one stem from there into the
 * gateway. Both layouts render; the container query picks one.
 */
function Band({ flow, count, columns, lit, tone, step, pulse }: BandProps) {
  const bus = BUS_Y[flow];
  const down = flow === "to-gateway";

  return (
    <div className="relative h-8 md:h-16 lg:h-20">
      {LAYOUTS.map((layout, i) => {
        const lanes = columnLanes(count, columns[i], lit);
        const active = lanes.filter((lane) => lane.lit);

        return (
          <div
            key={layout.key}
            className={`absolute inset-0 ${layout.className}`}
          >
            <div
              className="absolute inset-x-0"
              style={
                down
                  ? { top: 0, height: `${bus}%` }
                  : { top: `${bus}%`, bottom: 0 }
              }
            >
              {lanes.map((lane) => (
                <Elbow key={lane.key} lane={lane} flow={flow} tone={tone} />
              ))}
            </div>

            <span
              className="absolute left-1/2 block border-solid transition-colors duration-500"
              style={{
                borderLeftWidth: 1,
                borderColor: active.length > 0 ? wash(tone, 85) : MC.line,
                ...(down
                  ? { top: `${bus}%`, bottom: 0 }
                  : { top: 0, height: `${bus}%` }),
              }}
            />

            {pulse
              ? active.map((lane, index) => (
                  <Pulse
                    key={`${step}-${lane.key}`}
                    lane={lane}
                    flow={flow}
                    tone={tone}
                    animation={pulse(index)}
                  />
                ))
              : null}
          </div>
        );
      })}
    </div>
  );
}

export default function LayeredDiagram({
  gatewayLabel,
  compositionLine,
  clients,
  tiers,
  requests,
}: LayeredDiagramProps) {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const steps = requests.length * PHASE_LABEL.length;
  const step = useCycle(running, steps, PHASE_MS, restStep(requests.length));

  const phase = step % PHASE_LABEL.length;
  const request = requests[Math.floor(step / PHASE_LABEL.length)];
  const merging = phase === 2;
  const tone = merging ? MC.phosphor : MC.signal;

  const targets = tiers
    .map((node, i) => (request.targets.includes(node.name) ? i : -1))
    .filter((i) => i >= 0);
  const litNodes = phase === 0 ? [] : targets;

  const upstream =
    phase === 0
      ? () => anim(running, "mc-layer-in 1000ms linear both")
      : merging
        ? () => anim(running, "mc-layer-in 620ms linear 540ms both reverse")
        : null;

  const downstream =
    phase === 1
      ? (i: number) =>
          anim(running, `mc-layer-out 1000ms linear ${i * 70}ms both`)
      : merging
        ? (i: number) =>
            anim(running, `mc-layer-out 520ms linear ${i * 60}ms both reverse`)
        : null;

  return (
    <div
      ref={ref}
      className="@container relative w-full"
      aria-hidden="true"
      style={{ background: MC.bg }}
    >
      <style>{KEYFRAMES}</style>
      <div className="absolute inset-0" style={{ background: HUB_GLOW }} />

      <div className="px-4 py-4 sm:px-8 sm:py-8 md:px-12">
        <div className="mx-auto w-full max-w-7xl">
          <div className="grid grid-cols-2 gap-2 @min-[760px]:grid-cols-4">
            {clients.map((client, i) => (
              <Card
                key={client.key}
                title={client.label}
                detail={client.detail}
                lit={i === request.client}
                tone={tone}
              />
            ))}
          </div>

          <Band
            flow="to-gateway"
            count={clients.length}
            columns={CLIENT_COLUMNS}
            lit={[request.client]}
            tone={tone}
            step={step}
            pulse={upstream}
          />

          <div
            className="rounded-xl border px-3 py-2.5 md:px-5 md:py-3"
            style={{
              background: MC.panel,
              borderColor: wash(MC.phosphor, 45),
              boxShadow: `0 0 34px ${wash(MC.phosphor, 12)}`,
            }}
          >
            <div className="flex flex-wrap items-baseline justify-between gap-x-3 gap-y-1">
              <p
                className="font-heading uppercase"
                style={{
                  color: MC.ink,
                  fontSize: TYPE.h6,
                  letterSpacing: "0.22em",
                  lineHeight: LINE_HEIGHT,
                }}
              >
                {gatewayLabel}
              </p>
              <p
                className="font-mono uppercase"
                style={{
                  color: MC.dim,
                  fontSize: TYPE.label,
                  letterSpacing: "0.18em",
                  lineHeight: LINE_HEIGHT,
                }}
              >
                Gateway · distributed executor
              </p>
            </div>

            <div
              className="mt-2 flex flex-wrap items-center gap-x-2 gap-y-1 rounded-md border px-2 py-1"
              style={{ background: wash(MC.bg, 55), borderColor: MC.panelEdge }}
            >
              <span
                className="font-mono uppercase transition-colors duration-500"
                style={{
                  color: tone,
                  fontSize: TYPE.label,
                  letterSpacing: "0.18em",
                  lineHeight: LINE_HEIGHT,
                }}
              >
                {PHASE_LABEL[phase]}
              </span>
              <span
                className="font-mono"
                style={{
                  color: MC.ink,
                  fontSize: TYPE.label,
                  lineHeight: LINE_HEIGHT,
                }}
              >
                {request.operation}
              </span>
              <span
                className="font-mono"
                style={{
                  color: MC.dim,
                  fontSize: TYPE.label,
                  lineHeight: LINE_HEIGHT,
                }}
              >
                {`→ ${request.targets.join(" · ")}`}
              </span>
            </div>

            <p
              className="mt-1.5 font-mono"
              style={{
                color: MC.dim,
                fontSize: TYPE.label,
                lineHeight: LINE_HEIGHT,
              }}
            >
              {compositionLine}
            </p>
          </div>

          <Band
            flow="from-gateway"
            count={tiers.length}
            columns={NODE_COLUMNS}
            lit={litNodes}
            tone={tone}
            step={step}
            pulse={downstream}
          />

          <div className="grid grid-cols-3 gap-2 @max-[312px]:gap-x-1 @min-[760px]:grid-cols-7">
            {tiers.map((node, i) => (
              <Card
                key={node.name}
                title={node.name}
                detail={node.language}
                badge={node.badge}
                badgeColor={specColor(node.spec)}
                lit={litNodes.includes(i)}
                tone={tone}
              />
            ))}
          </div>

          <div className="mt-2 flex flex-wrap items-center justify-end gap-x-4 gap-y-1">
            {SPEC_LEGEND.map((spec) => (
              <p
                key={spec}
                className="flex items-center gap-1.5 font-mono"
                style={{
                  color: MC.dim,
                  fontSize: TYPE.label,
                  lineHeight: LINE_HEIGHT,
                }}
              >
                <span
                  className="block h-2 w-2"
                  style={{ background: specColor(spec), borderRadius: 2 }}
                />
                {`${specTag(spec)} = ${spec}`}
              </p>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
