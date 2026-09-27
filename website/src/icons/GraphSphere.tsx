"use client";

import { useEffect, useRef } from "react";
import type { ComponentPropsWithoutRef } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import {
  SPHERE_EDGES,
  SPHERE_VERTICES,
  VIEWBOX,
  lerp,
  projectVertex,
} from "./graphSphereGeometry";
import {
  DOT_PATHS,
  DOT_RADIUS,
  MAX_CONCURRENT_PULSES,
  ROTATION_PERIOD_MS,
  dotFrame,
  nodeMotion,
  pulseCandidates,
} from "./graphSphereMotion";
import { useGraphSphereClock } from "./useGraphSphereClock";

const PROJECTED = SPHERE_VERTICES.map((_, i) => projectVertex(i, 0));

const NODE_FAR_R = 2.2;
const NODE_NEAR_R = 5.6;
const NODE_FAR_ALPHA = 0.22;
const NODE_NEAR_ALPHA = 0.95;
const EDGE_FAR_ALPHA = 0.08;
const EDGE_NEAR_ALPHA = 0.55;
const EDGE_FAR_WIDTH = 0.5;
const EDGE_NEAR_WIDTH = 1.5;

const GLOW_FAR_SCALE = 2.4;
const GLOW_NEAR_SCALE = 3.5;
const GLOW_FAR_ALPHA = 0.1;
const GLOW_NEAR_ALPHA = 0.35;

const DOT_FAR_R = DOT_RADIUS * (NODE_FAR_R / NODE_NEAR_R);
const DOT_MAX_ALPHA = 0.85;
const DOT_FAR_ALPHA_FACTOR = NODE_FAR_ALPHA / NODE_NEAR_ALPHA;

function isTeal(index: number): boolean {
  return index % 4 === 0;
}

interface SphereNode {
  readonly vertexIndex: number;
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly alpha: number;
  readonly teal: boolean;
  readonly depth: number;
}

const NODES: readonly SphereNode[] = PROJECTED.map((p, i) => ({
  vertexIndex: i,
  x: p.x,
  y: p.y,
  r: lerp(NODE_FAR_R, NODE_NEAR_R, p.t),
  alpha: lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t),
  teal: isTeal(i),
  depth: p.t,
})).sort((a, b) => a.r - b.r);

const VERTEX_TO_NODE_IDX: number[] = new Array(SPHERE_VERTICES.length);
NODES.forEach((n, idx) => {
  VERTEX_TO_NODE_IDX[n.vertexIndex] = idx;
});

const EDGE_BANDS = 6;

interface EdgeGroup {
  readonly alpha: number;
  readonly width: number;
  readonly d: string;
}

function segmentPath(
  pairs: readonly (readonly [number, number])[],
  projected: readonly { readonly x: number; readonly y: number }[],
): string {
  return pairs
    .map(([i, j]) => {
      const a = projected[i];
      const b = projected[j];
      return `M${a.x.toFixed(1)},${a.y.toFixed(1)} L${b.x.toFixed(1)},${b.y.toFixed(1)}`;
    })
    .join(" ");
}

// Re-run every frame: band membership follows each edge's current depth, not its depth at theta=0.
function bucketEdgesByDepth(
  projected: readonly { readonly t: number }[],
): (readonly [number, number])[][] {
  const buckets: (readonly [number, number])[][] = Array.from(
    { length: EDGE_BANDS },
    () => [],
  );
  for (const [i, j] of SPHERE_EDGES) {
    const t = (projected[i].t + projected[j].t) / 2;
    const band = Math.min(EDGE_BANDS - 1, Math.floor(t * EDGE_BANDS));
    buckets[band].push([i, j]);
  }
  return buckets;
}

const EDGE_BAND_ALPHA: readonly number[] = Array.from(
  { length: EDGE_BANDS },
  (_, band) => lerp(EDGE_FAR_ALPHA, EDGE_NEAR_ALPHA, (band + 0.5) / EDGE_BANDS),
);
const EDGE_BAND_WIDTH: readonly number[] = Array.from(
  { length: EDGE_BANDS },
  (_, band) => lerp(EDGE_FAR_WIDTH, EDGE_NEAR_WIDTH, (band + 0.5) / EDGE_BANDS),
);

const EDGE_GROUPS: readonly EdgeGroup[] = bucketEdgesByDepth(PROJECTED).map(
  (pairs, band) => ({
    alpha: EDGE_BAND_ALPHA[band],
    width: EDGE_BAND_WIDTH[band],
    d: segmentPath(pairs, PROJECTED),
  }),
);

const SVG_NS = "http://www.w3.org/2000/svg";

type PaintRef =
  | { readonly kind: "node"; readonly idx: number }
  | { readonly kind: "dot"; readonly idx: number }
  | { readonly kind: "ring"; readonly idx: number };

const STATIC_PAINT_REFS: readonly PaintRef[] = NODES.map(
  (_, i) => ({ kind: "node", idx: i }) as const,
);

export function GraphSphere(props: ComponentPropsWithoutRef<"svg">) {
  const svgRef = useRef<SVGSVGElement | null>(null);
  const groupRefs = useRef<(SVGGElement | null)[]>([]);
  const nodeRefs = useRef<(SVGCircleElement | null)[]>([]);
  const haloRefs = useRef<(SVGCircleElement | null)[]>([]);
  const edgeRefs = useRef<(SVGPathElement | null)[]>([]);
  const dotRefs = useRef<SVGCircleElement[]>([]);
  const ringRefs = useRef<SVGCircleElement[]>([]);
  const paintRefsRef = useRef<readonly PaintRef[]>(STATIC_PAINT_REFS);
  const paintOrderRef = useRef<readonly number[]>([]);

  const reducedMotion = useReducedMotionPreference();

  useEffect(() => {
    if (reducedMotion) {
      return;
    }
    const svg = svgRef.current;
    if (!svg) {
      return;
    }
    const circles = DOT_PATHS.map(() => {
      const circle = document.createElementNS(SVG_NS, "circle");
      circle.setAttribute("r", DOT_RADIUS.toFixed(2));
      circle.setAttribute("fill", "var(--color-cc-accent)");
      circle.setAttribute("fill-opacity", "0");
      svg.appendChild(circle);
      return circle;
    });
    dotRefs.current = circles;
    const rings = Array.from({ length: MAX_CONCURRENT_PULSES }, () => {
      const circle = document.createElementNS(SVG_NS, "circle");
      circle.setAttribute("r", "0");
      circle.setAttribute("fill", "none");
      circle.setAttribute("stroke", "var(--color-cc-accent)");
      circle.setAttribute("stroke-opacity", "0");
      svg.appendChild(circle);
      return circle;
    });
    ringRefs.current = rings;
    paintRefsRef.current = [
      ...STATIC_PAINT_REFS,
      ...DOT_PATHS.map((_, idx) => ({ kind: "dot", idx }) as const),
      ...rings.map((_, idx) => ({ kind: "ring", idx }) as const),
    ];
    paintOrderRef.current = [];
    return () => {
      for (const circle of circles) {
        circle.remove();
      }
      for (const circle of rings) {
        circle.remove();
      }
      dotRefs.current = [];
      ringRefs.current = [];
      paintRefsRef.current = STATIC_PAINT_REFS;
      paintOrderRef.current = [];
    };
  }, [reducedMotion]);

  useGraphSphereClock(
    svgRef,
    (elapsedMs) => {
      const theta = ((elapsedMs / ROTATION_PERIOD_MS) % 1) * Math.PI * 2;
      const rawProjected = SPHERE_VERTICES.map((_, i) =>
        projectVertex(i, theta),
      );
      const pulses = pulseCandidates(elapsedMs);
      const pulseAgeByVertex = new Map(pulses.map((p) => [p.vertex, p.age]));

      const displayed = rawProjected.map((p, i) => {
        const motion = nodeMotion(
          i,
          elapsedMs,
          p.x,
          p.y,
          pulseAgeByVertex.get(i),
        );
        return { x: p.x + motion.dx, y: p.y + motion.dy, t: p.t, motion };
      });

      const nodeDepth: number[] = new Array(NODES.length);

      NODES.forEach((n, idx) => {
        const p = displayed[n.vertexIndex];
        nodeDepth[idx] = p.t;
        const node = nodeRefs.current[idx];
        if (node) {
          node.setAttribute("cx", p.x.toFixed(1));
          node.setAttribute("cy", p.y.toFixed(1));
          node.setAttribute("r", lerp(NODE_FAR_R, NODE_NEAR_R, p.t).toFixed(2));
          node.setAttribute(
            "fill-opacity",
            Math.min(
              1,
              lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t) + p.motion.brightness,
            ).toFixed(2),
          );
        }
        const halo = haloRefs.current[idx];
        if (halo) {
          const glowScale = lerp(GLOW_FAR_SCALE, GLOW_NEAR_SCALE, p.t);
          halo.setAttribute("cx", p.x.toFixed(1));
          halo.setAttribute("cy", p.y.toFixed(1));
          halo.setAttribute(
            "r",
            (lerp(NODE_FAR_R, NODE_NEAR_R, p.t) * glowScale).toFixed(1),
          );
          halo.setAttribute(
            "fill-opacity",
            lerp(GLOW_FAR_ALPHA, GLOW_NEAR_ALPHA, p.t).toFixed(2),
          );
        }
      });

      bucketEdgesByDepth(displayed).forEach((pairs, band) => {
        const path = edgeRefs.current[band];
        if (!path) {
          return;
        }
        path.setAttribute("d", segmentPath(pairs, displayed));
        path.setAttribute("stroke-opacity", EDGE_BAND_ALPHA[band].toFixed(2));
        path.setAttribute("stroke-width", EDGE_BAND_WIDTH[band].toFixed(2));
      });

      const dotDepth: number[] = new Array(DOT_PATHS.length);
      DOT_PATHS.forEach((dot, idx) => {
        const frame = dotFrame(dot, elapsedMs);
        const a = displayed[frame.fromVertex];
        const b = displayed[frame.toVertex];
        const depth = lerp(a.t, b.t, frame.frac);
        dotDepth[idx] = depth;
        const circle = dotRefs.current[idx];
        if (!circle) {
          return;
        }
        circle.setAttribute("cx", lerp(a.x, b.x, frame.frac).toFixed(1));
        circle.setAttribute("cy", lerp(a.y, b.y, frame.frac).toFixed(1));
        circle.setAttribute("r", lerp(DOT_FAR_R, DOT_RADIUS, depth).toFixed(2));
        circle.setAttribute(
          "fill-opacity",
          (
            frame.envelope *
            DOT_MAX_ALPHA *
            lerp(DOT_FAR_ALPHA_FACTOR, 1, depth)
          ).toFixed(2),
        );
      });

      const ringDepth: number[] = new Array(MAX_CONCURRENT_PULSES).fill(-1);
      pulses.forEach((pulse, slot) => {
        const ring = ringRefs.current[slot];
        const p = displayed[pulse.vertex];
        if (!ring || !p) {
          return;
        }
        const n = NODES[VERTEX_TO_NODE_IDX[pulse.vertex]];
        const baseR = lerp(NODE_FAR_R, NODE_NEAR_R, p.t);
        ring.setAttribute("cx", p.x.toFixed(1));
        ring.setAttribute("cy", p.y.toFixed(1));
        ring.setAttribute("r", (baseR * p.motion.ringScale).toFixed(2));
        ring.setAttribute("stroke-width", (baseR * 0.25).toFixed(2));
        ring.setAttribute(
          "stroke",
          n.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)",
        );
        ring.setAttribute(
          "stroke-opacity",
          (p.motion.ringAlpha * lerp(DOT_FAR_ALPHA_FACTOR, 1, p.t)).toFixed(2),
        );
        ringDepth[slot] = p.t;
      });
      for (let slot = pulses.length; slot < MAX_CONCURRENT_PULSES; slot++) {
        ringRefs.current[slot]?.setAttribute("stroke-opacity", "0");
      }

      const svg = svgRef.current;
      const refs = paintRefsRef.current;
      if (svg && refs.length > 0) {
        const depthOf = (ref: PaintRef) => {
          if (ref.kind === "dot") {
            return dotDepth[ref.idx];
          }
          if (ref.kind === "ring") {
            return ringDepth[ref.idx];
          }
          return nodeDepth[ref.idx];
        };
        const order = refs
          .map((_, i) => i)
          .sort((a, b) => depthOf(refs[a]) - depthOf(refs[b]));
        const prev = paintOrderRef.current;
        let changed = prev.length !== order.length;
        for (let i = 0; !changed && i < order.length; i++) {
          if (prev[i] !== order[i]) {
            changed = true;
          }
        }
        if (changed) {
          for (const i of order) {
            const ref = refs[i];
            const el =
              ref.kind === "node"
                ? groupRefs.current[ref.idx]
                : ref.kind === "dot"
                  ? dotRefs.current[ref.idx]
                  : ringRefs.current[ref.idx];
            if (el) {
              svg.appendChild(el);
            }
          }
          paintOrderRef.current = order;
        }
      }
    },
    !reducedMotion,
  );

  return (
    <svg
      ref={svgRef}
      viewBox={`0 0 ${VIEWBOX} ${VIEWBOX}`}
      aria-hidden="true"
      {...props}
    >
      <defs>
        <radialGradient id="graph-sphere-glow-accent">
          <stop
            offset="0%"
            stopColor="var(--color-cc-accent)"
            stopOpacity={1}
          />
          <stop
            offset="100%"
            stopColor="var(--color-cc-accent)"
            stopOpacity={0}
          />
        </radialGradient>
        <radialGradient id="graph-sphere-glow-success">
          <stop
            offset="0%"
            stopColor="var(--color-cc-success)"
            stopOpacity={1}
          />
          <stop
            offset="100%"
            stopColor="var(--color-cc-success)"
            stopOpacity={0}
          />
        </radialGradient>
      </defs>

      {EDGE_GROUPS.map((group, i) => (
        <path
          key={i}
          ref={(el) => {
            edgeRefs.current[i] = el;
          }}
          d={group.d}
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeOpacity={group.alpha.toFixed(2)}
          strokeWidth={group.width.toFixed(2)}
          strokeLinecap="round"
        />
      ))}

      {NODES.map((n, i) => {
        const color = n.teal
          ? "var(--color-cc-success)"
          : "var(--color-cc-accent)";
        const glowScale = lerp(GLOW_FAR_SCALE, GLOW_NEAR_SCALE, n.depth);
        const glowAlpha = lerp(GLOW_FAR_ALPHA, GLOW_NEAR_ALPHA, n.depth);
        return (
          <g
            key={i}
            ref={(el) => {
              groupRefs.current[i] = el;
            }}
          >
            <circle
              ref={(el) => {
                haloRefs.current[i] = el;
              }}
              cx={n.x.toFixed(1)}
              cy={n.y.toFixed(1)}
              r={(n.r * glowScale).toFixed(1)}
              fill={`url(#graph-sphere-glow-${n.teal ? "success" : "accent"})`}
              fillOpacity={glowAlpha.toFixed(2)}
            />
            <circle
              ref={(el) => {
                nodeRefs.current[i] = el;
              }}
              cx={n.x.toFixed(1)}
              cy={n.y.toFixed(1)}
              r={n.r.toFixed(2)}
              fill={color}
              fillOpacity={n.alpha.toFixed(2)}
            />
          </g>
        );
      })}
    </svg>
  );
}
