/**
 * The stream stage: shared vocabulary for the federation visuals, lifted from
 * the homepage FusionFlow section. Services are the canon five with their
 * exact colors; each visual stages colored streams that fall from labeled
 * square markers and converge into the glowing composition node, with one
 * output line continuing below. Pulses ride sampled stream curves via the
 * anim runtime.
 */

import type { Pt, Polyline } from "./anim";
import { measure } from "./anim";

export interface Service {
  readonly name: string;
  readonly color: string;
  readonly soft: string;
}

/** The canon services and colors from src/components/home/FusionFlow.tsx. */
export const CANON: readonly Service[] = [
  { name: "Catalog", color: "#f27765", soft: "#f9c2b8" },
  { name: "Billing", color: "#eabd21", soft: "#f6e39a" },
  { name: "Ordering", color: "#66be77", soft: "#bce5c4" },
  { name: "Shipping", color: "#00bce5", soft: "#a3e8f8" },
  { name: "User", color: "#a983ba", soft: "#dcc9e4" },
];

export const NODE_CORE = "#ffffff";
export const INK_DIM = "rgba(245,241,234,0.62)";
export const DASHED = "rgba(245,241,234,0.3)";

/** Sample a cubic bezier into a polyline the pulse runtime can ride. */
export function sampleCubic(
  p0: Pt,
  c1: Pt,
  c2: Pt,
  p1: Pt,
  n = 28,
): { readonly pts: readonly Pt[]; readonly d: string } {
  const pts: Pt[] = [];
  for (let i = 0; i <= n; i++) {
    const u = i / n;
    const v = 1 - u;
    const x =
      v * v * v * p0[0] +
      3 * v * v * u * c1[0] +
      3 * v * u * u * c2[0] +
      u * u * u * p1[0];
    const y =
      v * v * v * p0[1] +
      3 * v * v * u * c1[1] +
      3 * v * u * u * c2[1] +
      u * u * u * p1[1];
    pts.push([x, y]);
  }
  const d = `M${p0[0]} ${p0[1]} C ${c1[0]} ${c1[1]}, ${c2[0]} ${c2[1]}, ${p1[0]} ${p1[1]}`;
  return { pts, d };
}

export interface Stream {
  readonly d: string;
  readonly poly: Polyline;
  /** The same curve reversed, for pulses traveling back up. */
  readonly up: Polyline;
}

/**
 * A stream: falls straight from its marker, then bends toward the target.
 * Mirrors the homepage ribbons' straight-drop-then-curve silhouette.
 */
export function stream(
  x0: number,
  y0: number,
  target: Pt,
  drop = 0.35,
): Stream {
  const [tx, ty] = target;
  const h = ty - y0;
  const straight: Pt = [x0, y0 + h * drop];
  const c1: Pt = [x0, y0 + h * (drop + 0.42)];
  const c2: Pt = [tx, ty - h * 0.28];
  const end: Pt = [tx, ty];
  const { pts } = sampleCubic(straight, c1, c2, end);
  const full: Pt[] = [[x0, y0], ...pts];
  const poly = measure(full);
  const up = measure([...full].reverse());
  const d = `M${x0} ${y0} L${straight[0]} ${straight[1]} C ${c1[0]} ${c1[1]}, ${c2[0]} ${c2[1]}, ${end[0]} ${end[1]}`;
  return { d, poly, up };
}

interface MarkerProps {
  readonly x: number;
  readonly y: number;
  readonly color: string;
  readonly label: string;
  readonly labelSide?: "right" | "left";
}

/** The homepage's rounded-square source marker with its mono label. */
export function StreamMarker({
  x,
  y,
  color,
  label,
  labelSide = "right",
}: MarkerProps) {
  return (
    <g>
      <rect x={x - 6} y={y - 6} width={12} height={12} rx={3} fill={color} />
      <text
        x={labelSide === "right" ? x + 16 : x - 16}
        y={y + 4}
        textAnchor={labelSide === "right" ? "start" : "end"}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={11}
        letterSpacing="0.18em"
        fill={INK_DIM}
      >
        {label.toUpperCase()}
      </text>
    </g>
  );
}

interface GlowNodeProps {
  readonly x: number;
  readonly y: number;
  readonly id: string;
  readonly r?: number;
}

/** The composition node: soft halo, inner halo, crisp white core. */
export function GlowNode({ x, y, id, r = 9 }: GlowNodeProps) {
  return (
    <g>
      <defs>
        <radialGradient id={`${id}-halo`} cx="50%" cy="50%" r="50%">
          <stop offset="0.06" stopColor="#fff" stopOpacity="0.75" />
          <stop offset="0.45" stopColor="#fff" stopOpacity="0.12" />
          <stop offset="1" stopColor="#0e1522" stopOpacity="0" />
        </radialGradient>
      </defs>
      <circle cx={x} cy={y} r={r * 8} fill={`url(#${id}-halo)`} />
      <circle cx={x} cy={y} r={r} fill={NODE_CORE} />
    </g>
  );
}

interface GatewayChipProps {
  readonly x: number;
  readonly y: number;
  readonly label?: string;
  readonly w?: number;
}

/**
 * The runtime gateway as a quiet labeled chip. Deliberately NOT the white
 * glow node: the glow ("the Fusion sun") is reserved for composition.
 */
export function GatewayChip({
  x,
  y,
  label = "GATEWAY",
  w = 88,
}: GatewayChipProps) {
  return (
    <g>
      <rect
        x={x - w / 2}
        y={y - 13}
        width={w}
        height={26}
        rx={8}
        fill="#0d1424"
        stroke="rgba(94,234,212,0.45)"
      />
      <text
        x={x}
        y={y + 3.5}
        textAnchor="middle"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={9.5}
        letterSpacing="0.18em"
        fill="#5eead4"
      >
        {label}
      </text>
    </g>
  );
}

interface NodeCaptionProps {
  readonly x: number;
  readonly y: number;
  readonly label: string;
  /** Right edge of the dashed connector (usually the node's halo edge). */
  readonly toX: number;
}

/** The homepage's "FUSION COMPOSITION ----" caption beside the node. */
export function NodeCaption({ x, y, label, toX }: NodeCaptionProps) {
  return (
    <g>
      <text
        x={x}
        y={y + 3}
        textAnchor="end"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={10}
        letterSpacing="0.2em"
        fill={INK_DIM}
      >
        {label.toUpperCase()}
      </text>
      <line
        x1={x + 10}
        x2={toX}
        y1={y}
        y2={y}
        stroke={DASHED}
        strokeDasharray="4 5"
      />
    </g>
  );
}

interface SchemaLine {
  readonly code: string;
  readonly dim?: boolean;
}

interface SchemaCardProps {
  readonly x: number;
  readonly y: number;
  readonly w: number;
  readonly label: string;
  readonly color: string;
  readonly file?: string;
  readonly lines: readonly SchemaLine[];
}

/** Row baseline inside a SchemaCard for line index i. */
export function schemaRowY(y: number, i: number): number {
  return y + 48 + i * 18;
}

export function schemaCardHeight(lineCount: number): number {
  return 40 + lineCount * 18 + 12;
}

/**
 * A service's schema, as a quiet card in the stream theme: square color
 * marker + mono name in the header, SDL lines below. Content-bearing where
 * the streams alone cannot teach.
 */
export function SchemaCard({
  x,
  y,
  w,
  label,
  color,
  file = "schema.graphql",
  lines,
}: SchemaCardProps) {
  return (
    <g>
      <rect
        x={x}
        y={y}
        width={w}
        height={schemaCardHeight(lines.length)}
        rx={12}
        fill="rgba(12,19,34,0.5)"
        stroke="rgba(245,241,234,0.13)"
      />
      <rect x={x + 14} y={y + 12} width={10} height={10} rx={3} fill={color} />
      <text
        x={x + 32}
        y={y + 21}
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={10}
        letterSpacing="0.16em"
        fill={INK_DIM}
      >
        {label.toUpperCase()}
      </text>
      <text
        x={x + w - 14}
        y={y + 21}
        textAnchor="end"
        fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
        fontSize={8.5}
        fill={INK_DIM}
        opacity={0.6}
      >
        {file}
      </text>
      <line
        x1={x}
        x2={x + w}
        y1={y + 32}
        y2={y + 32}
        stroke="rgba(245,241,234,0.1)"
      />
      {lines.map((l, i) => (
        <text
          key={i}
          x={x + 16}
          y={schemaRowY(y, i)}
          xmlSpace="preserve"
          fontFamily="ui-monospace, SFMono-Regular, Menlo, monospace"
          fontSize={12}
          fill={l.dim ? INK_DIM : "#c9d4e8"}
          opacity={l.dim ? 0.7 : 1}
        >
          {l.code}
        </text>
      ))}
    </g>
  );
}
