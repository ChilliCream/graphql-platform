"use client";

import { useEffect, useMemo, useRef } from "react";
import type { ComponentPropsWithoutRef } from "react";

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
  ROTATION_PERIOD_MS,
  breatheScale,
  dotFrame,
  prefersReducedMotion,
} from "./graphSphereMotion";
import { useGraphSphereClock } from "./useGraphSphereClock";

// t: 0 farthest, 1 nearest -- the server-rendered frame is theta = 0.
const PROJECTED = SPHERE_VERTICES.map((_, i) => projectVertex(i, 0));

const NODE_FAR_R = 2.2;
const NODE_NEAR_R = 5.6;
const NODE_FAR_ALPHA = 0.22;
const NODE_NEAR_ALPHA = 0.95;
const EDGE_FAR_ALPHA = 0.08;
const EDGE_NEAR_ALPHA = 0.55;
const EDGE_FAR_WIDTH = 0.5;
const EDGE_NEAR_WIDTH = 1.5;
const HALO_COUNT = 3;

function isTeal(index: number): boolean {
  return index % 4 === 0;
}

const HALO_INDICES = new Set(
  PROJECTED.map((p, i) => [p.t, i] as const)
    .sort((a, b) => b[0] - a[0])
    .slice(0, HALO_COUNT)
    .map(([, i]) => i),
);

const HALO_ORDER = [...HALO_INDICES];

interface SphereNode {
  readonly vertexIndex: number;
  readonly x: number;
  readonly y: number;
  readonly r: number;
  readonly alpha: number;
  readonly teal: boolean;
  readonly haloId: number;
}

const NODES: readonly SphereNode[] = PROJECTED.map((p, i) => ({
  vertexIndex: i,
  x: p.x,
  y: p.y,
  r: lerp(NODE_FAR_R, NODE_NEAR_R, p.t),
  alpha: lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t),
  teal: isTeal(i),
  haloId: HALO_ORDER.indexOf(i),
})).sort((a, b) => a.r - b.r);

const EDGE_BANDS = 6;

interface EdgeGroup {
  readonly pairs: readonly (readonly [number, number])[];
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

const edgeBuckets: (readonly [number, number])[][] = Array.from(
  { length: EDGE_BANDS },
  () => [],
);
for (const [i, j] of SPHERE_EDGES) {
  const a = PROJECTED[i];
  const b = PROJECTED[j];
  const t = (a.t + b.t) / 2;
  const band = Math.min(EDGE_BANDS - 1, Math.floor(t * EDGE_BANDS));
  edgeBuckets[band].push([i, j]);
}

const EDGE_GROUPS: readonly EdgeGroup[] = edgeBuckets
  .map((pairs, band) => {
    const t = (band + 0.5) / EDGE_BANDS;
    return {
      pairs,
      alpha: lerp(EDGE_FAR_ALPHA, EDGE_NEAR_ALPHA, t),
      width: lerp(EDGE_FAR_WIDTH, EDGE_NEAR_WIDTH, t),
      d: segmentPath(pairs, PROJECTED),
    };
  })
  .filter((group) => group.d.length > 0);

const SVG_NS = "http://www.w3.org/2000/svg";
const HALO_BREATHE_PHASE_STEP = (2 * Math.PI) / HALO_COUNT;

export function GraphSphere(props: ComponentPropsWithoutRef<"svg">) {
  const svgRef = useRef<SVGSVGElement | null>(null);
  const nodeRefs = useRef<(SVGCircleElement | null)[]>([]);
  const haloRefs = useRef<(SVGCircleElement | null)[]>([]);
  const edgeRefs = useRef<(SVGPathElement | null)[]>([]);
  const dotRefs = useRef<SVGCircleElement[]>([]);

  // Read once: reduced-motion only needs the static frame, not a live toggle.
  const reducedMotion = useMemo(() => prefersReducedMotion(), []);

  // Dots have no server-rendered markup -- they exist only once JS runs and motion is allowed.
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
    return () => {
      for (const circle of circles) {
        circle.remove();
      }
      dotRefs.current = [];
    };
  }, [reducedMotion]);

  useGraphSphereClock(
    svgRef,
    (elapsedMs) => {
      const theta = ((elapsedMs / ROTATION_PERIOD_MS) % 1) * Math.PI * 2;
      const projected = SPHERE_VERTICES.map((_, i) => projectVertex(i, theta));

      NODES.forEach((n, idx) => {
        const p = projected[n.vertexIndex];
        const node = nodeRefs.current[idx];
        if (node) {
          node.setAttribute("cx", p.x.toFixed(1));
          node.setAttribute("cy", p.y.toFixed(1));
          node.setAttribute("r", lerp(NODE_FAR_R, NODE_NEAR_R, p.t).toFixed(2));
          node.setAttribute(
            "fill-opacity",
            lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t).toFixed(2),
          );
        }
        if (n.haloId >= 0) {
          const halo = haloRefs.current[idx];
          if (halo) {
            const r = lerp(NODE_FAR_R, NODE_NEAR_R, p.t);
            const breathe = breatheScale(
              n.haloId * HALO_BREATHE_PHASE_STEP,
              elapsedMs,
            );
            halo.setAttribute("cx", p.x.toFixed(1));
            halo.setAttribute("cy", p.y.toFixed(1));
            halo.setAttribute("r", (r * 3.5 * breathe).toFixed(1));
          }
        }
      });

      EDGE_GROUPS.forEach((group, idx) => {
        const path = edgeRefs.current[idx];
        if (!path) {
          return;
        }
        let sumT = 0;
        for (const [i, j] of group.pairs) {
          sumT += (projected[i].t + projected[j].t) / 2;
        }
        const avgT = sumT / group.pairs.length;
        path.setAttribute("d", segmentPath(group.pairs, projected));
        path.setAttribute(
          "stroke-opacity",
          lerp(EDGE_FAR_ALPHA, EDGE_NEAR_ALPHA, avgT).toFixed(2),
        );
        path.setAttribute(
          "stroke-width",
          lerp(EDGE_FAR_WIDTH, EDGE_NEAR_WIDTH, avgT).toFixed(2),
        );
      });

      DOT_PATHS.forEach((dot, idx) => {
        const circle = dotRefs.current[idx];
        if (!circle) {
          return;
        }
        const frame = dotFrame(dot, elapsedMs);
        const a = projected[frame.fromVertex];
        const b = projected[frame.toVertex];
        circle.setAttribute("cx", lerp(a.x, b.x, frame.frac).toFixed(1));
        circle.setAttribute("cy", lerp(a.y, b.y, frame.frac).toFixed(1));
        circle.setAttribute("fill-opacity", frame.alpha.toFixed(2));
      });
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
        {NODES.filter((n) => n.haloId >= 0).map((n) => (
          <radialGradient key={n.haloId} id={`graph-sphere-halo-${n.haloId}`}>
            <stop
              offset="0%"
              stopColor={
                n.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)"
              }
              stopOpacity={0.35}
            />
            <stop
              offset="100%"
              stopColor={
                n.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)"
              }
              stopOpacity={0}
            />
          </radialGradient>
        ))}
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
        return (
          <g key={i}>
            {n.haloId >= 0 && (
              <circle
                ref={(el) => {
                  haloRefs.current[i] = el;
                }}
                cx={n.x.toFixed(1)}
                cy={n.y.toFixed(1)}
                r={(n.r * 3.5).toFixed(1)}
                fill={`url(#graph-sphere-halo-${n.haloId})`}
              />
            )}
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
