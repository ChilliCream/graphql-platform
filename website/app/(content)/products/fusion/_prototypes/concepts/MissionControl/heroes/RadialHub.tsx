"use client";

import { useRef } from "react";
import type { CSSProperties, ReactNode } from "react";

import { Eyebrow } from "@/src/design-system/Eyebrow";

import { TYPE } from "../../../brand";
import { useElementMotion } from "../hooks";
import { MC, SOURCES, STATIONS, specTag } from "../palette";

/**
 * Hero visual: the radial hub. The Fusion gateway is the centre of the board,
 * the five subgraphs sit on an inner ring - each a plate with its name, the
 * language it is written in and the federation specification it implements,
 * on a ring segment tinted per specification - the two non-GraphQL sources sit
 * just outside it as smaller satellites, and the four clients form the outer
 * ring. Requests run along the spokes: in from a client, out to the subgraphs
 * that query needs, back to the centre, then out as one merged response.
 *
 * The nodes are DOM text, not SVG text, so their size is fixed in px and never
 * shrinks with the box: the 11px floor holds at 375px without letterboxing.
 * The SVG below them is square (`1000 x 1000` in a square box), so rings and
 * spokes scale uniformly and carry no lettering of their own.
 */

/** Square SVG board: rings and spokes only, ten user units per hub percent. */
const VIEW = 1000;
const C = VIEW / 2;

/** Ring radii, in percent of the hub square. */
const R_HUB = 15;
const R_SUB = 32;
const R_SOURCE = 40;
const R_CLIENT = 46;

/** Type sizes: `TYPE.label` is the floor, the hub grows the rest with the box. */
const NAME_SIZE = `clamp(${TYPE.label}px, 1.35svh, 15px)`;
const META_SIZE = `clamp(${TYPE.label}px, 1.1svh, 13px)`;

const GQL_FED = "GraphQL Federation";
const APOLLO_FED = "Apollo Federation";

/** Specification -> ring tint, so the two specs read as arcs of one circle. */
const SPEC_COLOR: Record<string, string> = {
  [GQL_FED]: MC.phosphor,
  [APOLLO_FED]: MC.signal,
};

/**
 * `CLIENTS` in `../palette` names the three clients the wall map plots; this
 * hero has to show four with the shape of each spelled out, so its roster is
 * local (see `./README.md`).
 */
const CLIENT_NODES = [
  { name: "Web app", note: "Browser", angle: -130 },
  { name: "Mobile app", note: "Native", angle: -50 },
  { name: "Partner API", note: "Server side", angle: 50 },
  { name: "AI agent", note: "Tool calls", angle: 130 },
] as const;

/**
 * `STATIONS` carries the cramped ops-room language tag ("JS/TS"); the plates
 * here have the room to name the language in full.
 */
const LANGUAGE: Record<string, string> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

/** Subgraph ring: one node per `STATIONS` entry, 72 degrees apart. */
const SUB_ANGLES = [-90, -18, 54, 126, 198];

/** The satellites sit in the ring gaps to the right and left of the centre. */
const SOURCE_ANGLES = [18, 162];

/** `SOURCES` names the feed; the language it is written in stays local. */
const SOURCE_LANGUAGE: Record<string, string> = {
  OpenAPI: "Python",
  gRPC: "Go",
};

const rad = (deg: number) => (deg * Math.PI) / 180;
const cos = (deg: number) => Math.cos(rad(deg));
const sin = (deg: number) => Math.sin(rad(deg));

/** Places a DOM node on a ring, in percentages of the hub square. */
function place(angle: number, r: number): CSSProperties {
  return {
    left: `${(50 + r * cos(angle)).toFixed(3)}%`,
    top: `${(50 + r * sin(angle)).toFixed(3)}%`,
    transform: "translate(-50%, -50%)",
  };
}

/** Ring arc between two angles, in SVG user units. */
function arc(from: number, to: number, r: number): string {
  const x0 = C + r * cos(from);
  const y0 = C + r * sin(from);
  const x1 = C + r * cos(to);
  const y1 = C + r * sin(to);
  return `M${x0.toFixed(2)} ${y0.toFixed(2)}A${r} ${r} 0 0 1 ${x1.toFixed(2)} ${y1.toFixed(2)}`;
}

/** Spoke from the rim of the gateway disc to the rim of a node. */
function spoke(angle: number, r: number, inset: number): string {
  const from = R_HUB * 10 + 10;
  const to = r * 10 - inset;
  return `M${(C + from * cos(angle)).toFixed(2)} ${(C + from * sin(angle)).toFixed(2)}L${(C + to * cos(angle)).toFixed(2)} ${(C + to * sin(angle)).toFixed(2)}`;
}

interface PlateProps {
  /** Ring placement, from `place`. */
  readonly style: CSSProperties;
  readonly children: ReactNode;
  /** Edge colour: the specification tint for a subgraph, the signal for a client. */
  readonly tone: string;
}

function Plate({ style, children, tone }: PlateProps) {
  return (
    <div
      className="absolute rounded-md border px-2 py-1 text-center"
      style={{
        ...style,
        background: MC.panel,
        borderColor: tone,
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

export default function RadialHub() {
  const ref = useRef<HTMLDivElement>(null);
  const running = useElementMotion(ref);

  return (
    <div ref={ref} className="absolute inset-0" aria-hidden="true">
      <div
        className="absolute inset-0"
        style={{
          background: `radial-gradient(60% 60% at 68% 50%, color-mix(in srgb, ${MC.phosphor} 12%, transparent), transparent)`,
        }}
      />

      <div className="absolute inset-0 flex items-center justify-center lg:justify-end lg:pr-[4%]">
        <div
          className="relative aspect-square"
          style={{ width: "min(100%, 88svh)" }}
        >
          <svg
            viewBox={`0 0 ${VIEW} ${VIEW}`}
            className="absolute inset-0 h-full w-full"
          >
            <circle
              cx={C}
              cy={C}
              r={R_CLIENT * 10}
              fill="none"
              stroke={MC.line}
              strokeOpacity="0.4"
              strokeDasharray="2 14"
            />
            <circle
              cx={C}
              cy={C}
              r={R_SOURCE * 10}
              fill="none"
              stroke={MC.line}
              strokeOpacity="0.22"
            />

            {STATIONS.map((station, i) => (
              <path
                key={`arc-${station.name}`}
                d={arc(SUB_ANGLES[i] - 34, SUB_ANGLES[i] + 34, R_SUB * 10)}
                fill="none"
                stroke={SPEC_COLOR[station.spec]}
                strokeOpacity="0.3"
                strokeWidth="26"
                strokeLinecap="round"
              />
            ))}

            {STATIONS.map((station, i) => (
              <path
                key={`spoke-${station.name}`}
                d={spoke(SUB_ANGLES[i], R_SUB, 62)}
                stroke={SPEC_COLOR[station.spec]}
                strokeOpacity="0.45"
              />
            ))}
            {SOURCES.map((source, i) => (
              <path
                key={`spoke-${source.name}`}
                d={spoke(SOURCE_ANGLES[i], R_SOURCE, 44)}
                stroke={MC.line}
                strokeOpacity="0.75"
                strokeDasharray="5 7"
              />
            ))}
            {CLIENT_NODES.map((client) => (
              <path
                key={`spoke-${client.name}`}
                d={spoke(client.angle, R_CLIENT, 50)}
                stroke={MC.signal}
                strokeOpacity="0.35"
              />
            ))}
          </svg>

          <div
            className="absolute flex flex-col items-center justify-center rounded-full border text-center"
            style={{
              left: "50%",
              top: "50%",
              width: `${R_HUB * 2}%`,
              height: `${R_HUB * 2}%`,
              transform: "translate(-50%, -50%)",
              background: MC.panel,
              borderColor: `color-mix(in srgb, ${MC.phosphor} 60%, transparent)`,
              fontFamily: MC.mono,
              letterSpacing: "0.12em",
              textTransform: "uppercase",
            }}
          >
            <span style={{ color: MC.ink, fontSize: NAME_SIZE }}>Fusion</span>
            <span style={{ color: MC.dim, fontSize: META_SIZE }}>Gateway</span>
            <span
              className="mt-1 px-2 leading-tight"
              style={{ color: MC.phosphor, fontSize: META_SIZE }}
            >
              Composite schema
            </span>
          </div>

          {STATIONS.map((station, i) => (
            <Plate
              key={station.name}
              style={place(SUB_ANGLES[i], R_SUB)}
              tone={`color-mix(in srgb, ${SPEC_COLOR[station.spec]} 55%, transparent)`}
            >
              <span
                className="block"
                style={{ color: MC.ink, fontSize: NAME_SIZE }}
              >
                {station.name}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {LANGUAGE[station.name]}
              </span>
              <span
                className="block"
                style={{
                  color: SPEC_COLOR[station.spec],
                  fontSize: META_SIZE,
                }}
              >
                {specTag(station.spec)}
              </span>
            </Plate>
          ))}

          {SOURCES.map((source, i) => (
            <Plate
              key={source.name}
              style={place(SOURCE_ANGLES[i], R_SOURCE)}
              tone={MC.panelEdge}
            >
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {source.name}
              </span>
              <span
                className="block"
                style={{ color: MC.ink, fontSize: META_SIZE }}
              >
                {source.kind}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {SOURCE_LANGUAGE[source.kind]}
              </span>
            </Plate>
          ))}

          {CLIENT_NODES.map((client) => (
            <Plate
              key={client.name}
              style={place(client.angle, R_CLIENT)}
              tone={`color-mix(in srgb, ${MC.signal} 45%, transparent)`}
            >
              <span
                className="block"
                style={{ color: MC.ink, fontSize: NAME_SIZE }}
              >
                {client.name}
              </span>
              <span
                className="block"
                style={{ color: MC.dim, fontSize: META_SIZE }}
              >
                {client.note}
              </span>
            </Plate>
          ))}
        </div>
      </div>

      <div className="border-cc-card-border bg-cc-card-bg absolute right-4 bottom-4 rounded-md border px-3 py-2 sm:right-8 sm:bottom-8">
        <Eyebrow color="ink-dim">Composite schema</Eyebrow>
        <ul
          className="mt-2 space-y-1"
          style={{ fontFamily: MC.mono, fontSize: META_SIZE }}
        >
          {[GQL_FED, APOLLO_FED].map((spec) => (
            <li key={spec} className="flex items-center gap-2">
              <span
                className="inline-block h-2 w-6 rounded-full"
                style={{ background: SPEC_COLOR[spec] }}
              />
              <span style={{ color: MC.ink }}>{spec}</span>
            </li>
          ))}
        </ul>
        <p
          className="mt-2"
          style={{ fontFamily: MC.mono, fontSize: META_SIZE, color: MC.dim }}
        >
          {`${STATIONS.length} subgraphs · ${SOURCES.length} sources`}
        </p>
        <p
          className="mt-1"
          style={{
            fontFamily: MC.mono,
            fontSize: META_SIZE,
            color: running ? MC.phosphor : MC.dim,
          }}
        >
          resting
        </p>
      </div>
    </div>
  );
}
