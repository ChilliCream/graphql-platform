"use client";

import { useRef } from "react";

import { anim, useCycle, useElementMotion } from "./hooks";
import { MC, protocolColor, protocolTag, specTag } from "./palette";
import type { StationSpec } from "./palette";
import { BRAND, TYPE } from "./tokens";
import {
  BUS_Y,
  BUS_Y_CENTERED,
  PHASE_LABEL,
  PHASE_MS,
  SPEC_LEGEND,
  columnLanes,
  restStep,
} from "./diagram";
import type { BandFlow, ClientNode, Request, TierNode } from "./diagram";
import { Card, Elbow, LINE_HEIGHT, Pulse, wash } from "./parts";

interface LayeredDiagramProps {
  readonly gatewayLabel: string;
  readonly caption?: string;
  /** Omit to drop the composition line, closing the router panel up. */
  readonly compositionLine?: string;
  readonly clients: readonly ClientNode[];
  readonly tiers: readonly TierNode[];
  readonly requests: readonly Request[];
  /** Fusion's opt-in dense mode: a lower one-row threshold and tighter cards. */
  readonly dense?: boolean;
  readonly busCentered?: boolean;
  /** Fusion's opt-in: legend badges read the short spec tag, with no "FED". */
  readonly shortSpecTags?: boolean;
}

const CLIENT_COLUMNS = [2, 4] as const;
const NODE_COLUMNS = [3, 7] as const;

/** Wide-layout grid gap in dense mode, in px; tied to `DENSE_WIDE_GAP_CLASS`. */
const DENSE_WIDE_GAP = 4;
const DENSE_WIDE_GAP_CLASS = "@min-[640px]:gap-1";

const LAYOUTS = [
  { key: "stacked", className: "@min-[760px]:hidden" },
  { key: "wide", className: "hidden @min-[760px]:block" },
] as const;

/** Fusion's dense mode switches to the wide layout at a narrower container width. */
const DENSE_LAYOUTS = [
  { key: "stacked", className: "@min-[640px]:hidden" },
  { key: "wide", className: "hidden @min-[640px]:block" },
] as const;

function layerKeyframes(busY: Readonly<Record<BandFlow, number>>): string {
  return `
@keyframes mc-layer-in {
  0% { top: 0%; left: var(--mc-layer-x); opacity: 0; }
  10% { opacity: 1; }
  50% { top: ${busY["to-gateway"]}%; left: var(--mc-layer-x); }
  78% { top: ${busY["to-gateway"]}%; left: 50%; }
  100% { top: 100%; left: 50%; opacity: 1; }
}
@keyframes mc-layer-out {
  0% { top: 0%; left: 50%; opacity: 1; }
  22% { top: ${busY["from-gateway"]}%; left: 50%; }
  50% { top: ${busY["from-gateway"]}%; left: var(--mc-layer-x); }
  92% { opacity: 1; }
  100% { top: 100%; left: var(--mc-layer-x); opacity: 0; }
}
`;
}

const HUB_GLOW = `radial-gradient(48% 36% at 50% 50%, ${wash(MC.phosphor, 16)} 0%, transparent 72%)`;

function specColor(spec: StationSpec | null): string {
  if (spec === null) return MC.dim;
  return spec === "Apollo Federation" ? BRAND.violet : MC.phosphor;
}

interface BandProps {
  readonly flow: BandFlow;
  readonly count: number;
  readonly columns: readonly [number, number];
  readonly lit: readonly number[];
  readonly tone: string;
  readonly step: number;
  readonly pulse: ((index: number) => string) | null;
  readonly dense?: boolean;
  readonly busY: Readonly<Record<BandFlow, number>>;
}

/** Both layouts always render; the container query picks which one shows. */
function Band({
  flow,
  count,
  columns,
  lit,
  tone,
  step,
  pulse,
  dense = false,
  busY,
}: BandProps) {
  const bus = busY[flow];
  const down = flow === "to-gateway";
  const layouts = dense ? DENSE_LAYOUTS : LAYOUTS;

  return (
    <div className="relative h-8 md:h-16 lg:h-20">
      {layouts.map((layout, i) => {
        const gap = dense && i === 1 ? DENSE_WIDE_GAP : undefined;
        const lanes = columnLanes(count, columns[i], lit, gap);
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
                    tone={tone}
                    animation={pulse(index)}
                    busY={bus}
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
  caption = "Gateway · distributed executor",
  compositionLine,
  clients,
  tiers,
  requests,
  dense = false,
  busCentered = false,
  shortSpecTags = false,
}: LayeredDiagramProps) {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);
  const steps = requests.length * PHASE_LABEL.length;
  const step = useCycle(running, steps, PHASE_MS, restStep(requests.length));
  const busY = busCentered ? BUS_Y_CENTERED : BUS_Y;

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
      <style>{layerKeyframes(busY)}</style>
      <div className="absolute inset-0" style={{ background: HUB_GLOW }} />

      <div
        className={
          dense
            ? "px-4 py-4 sm:px-8 sm:py-8 md:px-12 @min-[640px]:px-6"
            : "px-4 py-4 sm:px-8 sm:py-8 md:px-12"
        }
      >
        <div className="mx-auto w-full max-w-7xl">
          <div
            className={
              dense
                ? `grid grid-cols-2 gap-2 @min-[640px]:grid-cols-4 ${DENSE_WIDE_GAP_CLASS}`
                : "grid grid-cols-2 gap-2 @min-[760px]:grid-cols-4"
            }
          >
            {clients.map((client, i) => (
              <Card
                key={client.key}
                title={client.label}
                detail={client.detail}
                badge={
                  client.protocol ? protocolTag(client.protocol) : undefined
                }
                badgeColor={
                  client.protocol ? protocolColor(client.protocol) : undefined
                }
                lit={i === request.client}
                tone={tone}
                dense={dense}
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
            dense={dense}
            busY={busY}
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
                {caption}
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
              {request.protocol ? (
                <span
                  className="font-mono uppercase"
                  style={{
                    color: protocolColor(request.protocol),
                    fontSize: TYPE.label,
                    letterSpacing: "0.1em",
                    lineHeight: LINE_HEIGHT,
                  }}
                >
                  {protocolTag(request.protocol)}
                </span>
              ) : null}
              <span
                className="font-mono"
                style={{
                  color: MC.ink,
                  fontSize: TYPE.label,
                  lineHeight: LINE_HEIGHT,
                }}
              >
                {request.entry
                  ? `${request.entry} → ${request.operation}`
                  : request.operation}
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

            {compositionLine ? (
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
            ) : null}
          </div>

          <Band
            flow="from-gateway"
            count={tiers.length}
            columns={NODE_COLUMNS}
            lit={litNodes}
            tone={tone}
            step={step}
            pulse={downstream}
            dense={dense}
            busY={busY}
          />

          <div
            className={
              dense
                ? `grid grid-cols-3 gap-2 @max-[312px]:gap-x-1 @min-[640px]:grid-cols-7 ${DENSE_WIDE_GAP_CLASS}`
                : "grid grid-cols-3 gap-2 @max-[312px]:gap-x-1 @min-[760px]:grid-cols-7"
            }
          >
            {tiers.map((node, i) => (
              <Card
                key={node.name}
                title={node.name}
                detail={node.language}
                badge={node.badge}
                badgeColor={specColor(node.spec)}
                lit={litNodes.includes(i)}
                tone={tone}
                dense={dense}
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
                {`${specTag(spec, shortSpecTags)} = ${spec}`}
              </p>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
