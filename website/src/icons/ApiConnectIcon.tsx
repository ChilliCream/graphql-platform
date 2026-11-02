import { useId } from "react";
import type { CSSProperties } from "react";

import {
  ICOSAHEDRON_EDGES,
  ICOSAHEDRON_VERTEX_COUNT,
  lerp,
  projectIcosahedronVertex,
} from "./graphSphereGeometry";

interface ApiConnectIconProps {
  readonly className?: string;
  readonly style?: CSSProperties;
}

const COLOR = "var(--color-cc-accent)";
const TILE_FROM =
  "color-mix(in srgb, var(--color-cc-accent) 28%, var(--color-cc-surface))";
const TILE_TO =
  "color-mix(in srgb, var(--color-cc-accent) 10%, var(--color-cc-surface))";
const BORDER_BRIGHT =
  "color-mix(in srgb, var(--color-cc-accent) 80%, var(--color-cc-white))";
const BORDER_DIM =
  "color-mix(in srgb, var(--color-cc-accent) 20%, transparent)";
const STROKE_BRIGHT =
  "color-mix(in srgb, var(--color-cc-accent) 55%, var(--color-cc-white))";

const SPHERE_CENTER = 40;
const SPHERE_CAM_K = 65;
const SPHERE_CAM_DIST = 3;

const NODE_R = 2.6;
const NODE_FAR_ALPHA = 0.35;
const NODE_NEAR_ALPHA = 1;
const EDGE_FAR_ALPHA = 0.12;
const EDGE_NEAR_ALPHA = 0.85;
const EDGE_WIDTH_FAR = 1;
const EDGE_WIDTH_NEAR = 2.2;
const GLOW_RADIUS = NODE_R * 2.6;

const SPHERE_NODES = Array.from({ length: ICOSAHEDRON_VERTEX_COUNT }, (_, i) =>
  projectIcosahedronVertex(i, SPHERE_CENTER, SPHERE_CAM_K, SPHERE_CAM_DIST),
);

const SPHERE_EDGES = ICOSAHEDRON_EDGES.map(([a, b]) => ({
  x1: SPHERE_NODES[a].x,
  y1: SPHERE_NODES[a].y,
  x2: SPHERE_NODES[b].x,
  y2: SPHERE_NODES[b].y,
  t: (SPHERE_NODES[a].t + SPHERE_NODES[b].t) / 2,
}));

const NODE_DRAW_ORDER = SPHERE_NODES.map((_, i) => i).sort(
  (a, b) => SPHERE_NODES[a].t - SPHERE_NODES[b].t,
);

const GLOW_NODE_INDICES = new Set([0, 6, 9]);

/** A simplified, static echo of the closing band's GraphSphere, for "Connect every API". */
export function ApiConnectIcon({ className, style }: ApiConnectIconProps) {
  const uid = useId();
  const tile = `api-connect-tile-${uid}`;
  const border = `api-connect-border-${uid}`;
  const glow = `api-connect-glow-${uid}`;
  const inner = `api-connect-inner-${uid}`;
  const stroke = `api-connect-stroke-${uid}`;
  const nodeGlow = `api-connect-node-glow-${uid}`;

  return (
    <svg
      viewBox="0 0 80 80"
      fill="none"
      aria-hidden="true"
      className={className}
      style={style}
    >
      <defs>
        <linearGradient
          id={tile}
          x1="8"
          y1="8"
          x2="72"
          y2="72"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={TILE_FROM} />
          <stop offset="1" stopColor={TILE_TO} />
        </linearGradient>
        <linearGradient
          id={border}
          x1="8"
          y1="8"
          x2="72"
          y2="72"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={BORDER_BRIGHT} />
          <stop offset="1" stopColor={BORDER_DIM} />
        </linearGradient>
        <linearGradient
          id={stroke}
          x1="20"
          y1="20"
          x2="60"
          y2="60"
          gradientUnits="userSpaceOnUse"
        >
          <stop offset="0" stopColor={STROKE_BRIGHT} />
          <stop offset="1" stopColor={COLOR} />
        </linearGradient>
        <radialGradient id={inner} cx="30%" cy="24%" r="60%">
          <stop offset="0" stopColor={STROKE_BRIGHT} stopOpacity="0.35" />
          <stop offset="1" stopColor={STROKE_BRIGHT} stopOpacity="0" />
        </radialGradient>
        <filter id={glow} x="-60%" y="-60%" width="220%" height="220%">
          <feGaussianBlur stdDeviation="5" />
        </filter>
        <filter id={nodeGlow} x="-150%" y="-150%" width="400%" height="400%">
          <feGaussianBlur stdDeviation="1.1" />
        </filter>
      </defs>

      <rect
        x="8"
        y="8"
        width="64"
        height="64"
        rx="16"
        fill={COLOR}
        opacity="0.3"
        filter={`url(#${glow})`}
      />
      <rect
        x="4"
        y="4"
        width="72"
        height="72"
        rx="18"
        fill={`url(#${tile})`}
        stroke={`url(#${border})`}
        strokeWidth="1.5"
      />
      <rect
        x="4"
        y="4"
        width="72"
        height="72"
        rx="18"
        fill={`url(#${inner})`}
      />

      {SPHERE_EDGES.map((edge, i) => (
        <line
          key={i}
          x1={edge.x1.toFixed(1)}
          y1={edge.y1.toFixed(1)}
          x2={edge.x2.toFixed(1)}
          y2={edge.y2.toFixed(1)}
          stroke={`url(#${stroke})`}
          strokeWidth={lerp(EDGE_WIDTH_FAR, EDGE_WIDTH_NEAR, edge.t).toFixed(2)}
          strokeLinecap="round"
          strokeOpacity={lerp(EDGE_FAR_ALPHA, EDGE_NEAR_ALPHA, edge.t).toFixed(
            2,
          )}
        />
      ))}

      {NODE_DRAW_ORDER.map((i) => {
        const node = SPHERE_NODES[i];
        return (
          <g key={i}>
            {GLOW_NODE_INDICES.has(i) && (
              <circle
                cx={node.x.toFixed(1)}
                cy={node.y.toFixed(1)}
                r={GLOW_RADIUS.toFixed(1)}
                fill={COLOR}
                opacity="0.5"
                filter={`url(#${nodeGlow})`}
              />
            )}
            <circle
              cx={node.x.toFixed(1)}
              cy={node.y.toFixed(1)}
              r={NODE_R}
              fill={`url(#${stroke})`}
              fillOpacity={lerp(
                NODE_FAR_ALPHA,
                NODE_NEAR_ALPHA,
                node.t,
              ).toFixed(2)}
            />
          </g>
        );
      })}
    </svg>
  );
}
