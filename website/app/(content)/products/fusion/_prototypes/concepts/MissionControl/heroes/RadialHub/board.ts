/**
 * Radial hub geometry, roster and timing: everything the hero draws but no
 * markup, so `index.tsx` stays a single readable render pass.
 *
 * The board is a square `1000 x 1000` SVG in a square box, so rings and spokes
 * scale uniformly; the nodes on top of it are DOM text placed by `place`, in
 * percentages of the same square.
 */

import type { CSSProperties } from "react";

import { TYPE } from "../../../../brand";
import { MC } from "../../palette";

/** Square SVG board: rings and spokes only, ten user units per hub percent. */
export const VIEW = 1000;
export const C = VIEW / 2;

/** Ring radii, in percent of the hub square. */
export const R_HUB = 15;
export const R_SUB = 32;
export const R_SOURCE = 40;
export const R_CLIENT = 46;

/** Type sizes: `TYPE.label` is the floor, the hub grows the rest with the box. */
export const NAME_SIZE = `clamp(${TYPE.label}px, 1.35svh, 15px)`;
export const META_SIZE = `clamp(${TYPE.label}px, 1.1svh, 13px)`;

export const GQL_FED = "GraphQL Federation";
export const APOLLO_FED = "Apollo Federation";

/** Specification -> ring tint, so the two specs read as arcs of one circle. */
export const SPEC_COLOR: Record<string, string> = {
  [GQL_FED]: MC.phosphor,
  [APOLLO_FED]: MC.signal,
};

/**
 * `CLIENTS` in `../palette` names the three clients the wall map plots; this
 * hero has to show four with the shape of each spelled out, so its roster is
 * local (see `../README.md`).
 */
export const CLIENT_NODES = [
  { name: "Web app", note: "Browser", angle: -130 },
  { name: "Mobile app", note: "Native", angle: -50 },
  { name: "Partner API", note: "Server side", angle: 50 },
  { name: "AI agent", note: "Tool calls", angle: 130 },
] as const;

/**
 * `STATIONS` carries the cramped ops-room language tag ("JS/TS"); the plates
 * here have the room to name the language in full.
 */
export const LANGUAGE: Record<string, string> = {
  Catalog: "TypeScript",
  Billing: "Java",
  Ordering: "Go",
  Shipping: "Ruby",
  Accounts: "C#",
};

/** Subgraph ring: one node per `STATIONS` entry, 72 degrees apart. */
export const SUB_ANGLES = [-90, -18, 54, 126, 198];

/** The satellites sit in the ring gaps to the right and left of the centre. */
export const SOURCE_ANGLES = [18, 162];

/** `SOURCES` names the feed; the language it is written in stays local. */
export const SOURCE_LANGUAGE: Record<string, string> = {
  OpenAPI: "Python",
  gRPC: "Go",
};

/**
 * One request per step: the client that sends it and the subgraphs its query
 * needs. Step 0 is the rest frame, so it is the one the server render, the
 * reduced-motion render and the off-screen render show.
 */
export const FLIGHTS = [
  { client: 0, targets: [0, 2] },
  { client: 1, targets: [0, 3, 2] },
  { client: 2, targets: [1, 4] },
  { client: 3, targets: [0, 4, 1] },
] as const;
export const REST_STEP = 0;

/** Where a spoke starts and ends, in SVG user units. */
export const HUB_EDGE = R_HUB * 10 + 10;
export const SUB_EDGE = R_SUB * 10 - 62;
export const CLIENT_EDGE = R_CLIENT * 10 - 50;

/**
 * One step of the request: in from the client, out to the subgraphs it needs,
 * back from all of them at the same moment - the merge - then out again as one
 * response.
 */
export const STEP_MS = 3400;
export const REQUEST = { delay: 0, duration: 850 } as const;
export const FAN_OUT = { delay: 950, duration: 800, stagger: 120 } as const;
export const RETURN = { delay: 2000, duration: 750 } as const;
export const MERGE = { delay: 2700, duration: 700 } as const;
export const RESPONSE = { delay: 2800, duration: 600 } as const;

export const KEYFRAMES = `
@keyframes rh-run {
  0% { transform: translateX(0); opacity: 0; }
  12% { opacity: 1; }
  84% { opacity: 1; }
  100% { transform: translateX(var(--rh-d)); opacity: 0; }
}
@keyframes rh-merge {
  0% { transform: scale(1); opacity: 0; }
  30% { opacity: 0.7; }
  100% { transform: scale(1.5); opacity: 0; }
}
@keyframes rh-spin { to { transform: rotate(360deg); } }
@keyframes rh-spin-back { to { transform: rotate(-360deg); } }
`;

const rad = (deg: number) => (deg * Math.PI) / 180;
const cos = (deg: number) => Math.cos(rad(deg));
const sin = (deg: number) => Math.sin(rad(deg));

/** Places a DOM node on a ring, in percentages of the hub square. */
export function place(angle: number, r: number): CSSProperties {
  return {
    left: `${(50 + r * cos(angle)).toFixed(3)}%`,
    top: `${(50 + r * sin(angle)).toFixed(3)}%`,
    transform: "translate(-50%, -50%)",
  };
}

/** Ring arc between two angles, in SVG user units. */
export function arc(from: number, to: number, r: number): string {
  const x0 = C + r * cos(from);
  const y0 = C + r * sin(from);
  const x1 = C + r * cos(to);
  const y1 = C + r * sin(to);
  return `M${x0.toFixed(2)} ${y0.toFixed(2)}A${r} ${r} 0 0 1 ${x1.toFixed(2)} ${y1.toFixed(2)}`;
}

/** Spoke from the rim of the gateway disc to the rim of a node. */
export function spoke(angle: number, r: number, inset: number): string {
  const from = R_HUB * 10 + 10;
  const to = r * 10 - inset;
  return `M${(C + from * cos(angle)).toFixed(2)} ${(C + from * sin(angle)).toFixed(2)}L${(C + to * cos(angle)).toFixed(2)} ${(C + to * sin(angle)).toFixed(2)}`;
}
