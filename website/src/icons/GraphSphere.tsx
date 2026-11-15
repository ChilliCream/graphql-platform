"use client";

import { useEffect, useId, useRef } from "react";
import type { ComponentPropsWithoutRef } from "react";

import { useReducedMotionPreference } from "@/src/nitro/lib/motion";

import {
  VIEWBOX,
  graphForVariant,
  lerp,
  projectVertex,
} from "./graphSphereGeometry";
import type {
  GraphGeometry,
  GraphVariant,
  ProjectedVertex,
} from "./graphSphereGeometry";
import {
  DOT_RADIUS,
  MAX_CONCURRENT_PULSES,
  ROTATION_PERIOD_MS,
  createGraphMotion,
} from "./graphSphereMotion";
import type { GraphMotion } from "./graphSphereMotion";
import { useGraphSphereClock } from "./useGraphSphereClock";

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
const EDGE_BANDS = 6;
const SVG_NS = "http://www.w3.org/2000/svg";

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

interface EdgeGroup {
  readonly alpha: number;
  readonly width: number;
  readonly d: string;
}

type PaintRef =
  | { readonly kind: "node"; readonly idx: number }
  | { readonly kind: "dot"; readonly idx: number }
  | { readonly kind: "ring"; readonly idx: number };

interface GraphState {
  readonly graph: GraphGeometry;
  readonly motion: GraphMotion;
  readonly projected: readonly ProjectedVertex[];
  readonly nodes: readonly SphereNode[];
  readonly vertexToNodeIdx: readonly number[];
  readonly edgeGroups: readonly EdgeGroup[];
  readonly staticPaintRefs: readonly PaintRef[];
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

function bucketEdgesByDepth(
  pairs: readonly (readonly [number, number])[],
  projected: readonly { readonly t: number }[],
): (readonly [number, number])[][] {
  const buckets: (readonly [number, number])[][] = Array.from(
    { length: EDGE_BANDS },
    () => [],
  );
  for (const [i, j] of pairs) {
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

function createGraphState(variant: GraphVariant): GraphState {
  const graph = graphForVariant(variant);
  const projected = graph.vertices.map((_, i) => projectVertex(graph, i, 0));
  const nodes: readonly SphereNode[] = projected
    .map((p, i) => ({
      vertexIndex: i,
      x: p.x,
      y: p.y,
      r: lerp(NODE_FAR_R, NODE_NEAR_R, p.t),
      alpha: lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, p.t),
      teal: isTeal(i),
      depth: p.t,
    }))
    .sort((a, b) => a.r - b.r);
  const vertexToNodeIdx: number[] = new Array(graph.vertices.length);
  nodes.forEach((node, index) => {
    vertexToNodeIdx[node.vertexIndex] = index;
  });
  const edgeGroups = bucketEdgesByDepth(graph.edges, projected).map(
    (pairs, band) => ({
      alpha: EDGE_BAND_ALPHA[band],
      width: EDGE_BAND_WIDTH[band],
      d: segmentPath(pairs, projected),
    }),
  );

  return {
    graph,
    motion: createGraphMotion(graph),
    projected,
    nodes,
    vertexToNodeIdx,
    edgeGroups,
    staticPaintRefs: nodes.map(
      (_, index) => ({ kind: "node", idx: index }) as const,
    ),
  };
}

const GRAPH_STATES: Readonly<Record<GraphVariant, GraphState>> = {
  sphere: createGraphState("sphere"),
  faceted: createGraphState("faceted"),
};

interface GraphSphereProps extends ComponentPropsWithoutRef<"svg"> {
  readonly variant?: GraphVariant;
}

export function GraphSphere({
  variant = "sphere",
  ...props
}: GraphSphereProps) {
  const state = GRAPH_STATES[variant];
  const svgRef = useRef<SVGSVGElement | null>(null);
  const groupRefs = useRef<(SVGGElement | null)[]>([]);
  const nodeRefs = useRef<(SVGCircleElement | null)[]>([]);
  const haloRefs = useRef<(SVGCircleElement | null)[]>([]);
  const edgeRefs = useRef<(SVGPathElement | null)[]>([]);
  const dotRefs = useRef<SVGCircleElement[]>([]);
  const ringRefs = useRef<SVGCircleElement[]>([]);
  const { staticPaintRefs } = state;
  const paintRefsRef = useRef<readonly PaintRef[]>(staticPaintRefs);
  const paintOrderRef = useRef<readonly number[]>([]);
  const reducedMotion = useReducedMotionPreference();
  const instanceId = useId();
  const accentGlowId = `graph-sphere-glow-accent-${instanceId}`;
  const successGlowId = `graph-sphere-glow-success-${instanceId}`;

  useEffect(() => {
    if (reducedMotion) {
      return;
    }
    const svg = svgRef.current;
    if (!svg) {
      return;
    }
    const circles = state.motion.dotPaths.map(() => {
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
      ...staticPaintRefs,
      ...state.motion.dotPaths.map(
        (_, index) => ({ kind: "dot", idx: index }) as const,
      ),
      ...rings.map((_, index) => ({ kind: "ring", idx: index }) as const),
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
      paintRefsRef.current = staticPaintRefs;
      paintOrderRef.current = [];
    };
  }, [reducedMotion, state, staticPaintRefs]);

  useGraphSphereClock(
    svgRef,
    (elapsedMs) => {
      const theta = ((elapsedMs / ROTATION_PERIOD_MS) % 1) * Math.PI * 2;
      const rawProjected = state.graph.vertices.map((_, i) =>
        projectVertex(state.graph, i, theta),
      );
      const pulses = state.motion.pulseCandidates(elapsedMs);
      const pulseAgeByVertex = new Map(
        pulses.map((pulse) => [pulse.vertex, pulse.age]),
      );
      const displayed = rawProjected.map((point, index) => {
        const motion = state.motion.nodeMotion(
          index,
          elapsedMs,
          point.x,
          point.y,
          pulseAgeByVertex.get(index),
        );
        return {
          x: point.x + motion.dx,
          y: point.y + motion.dy,
          t: point.t,
          motion,
        };
      });
      const nodeDepth: number[] = new Array(state.nodes.length);

      state.nodes.forEach((nodeData, index) => {
        const point = displayed[nodeData.vertexIndex];
        nodeDepth[index] = point.t;
        const node = nodeRefs.current[index];
        if (node) {
          node.setAttribute("cx", point.x.toFixed(1));
          node.setAttribute("cy", point.y.toFixed(1));
          node.setAttribute(
            "r",
            lerp(NODE_FAR_R, NODE_NEAR_R, point.t).toFixed(2),
          );
          node.setAttribute(
            "fill-opacity",
            Math.min(
              1,
              lerp(NODE_FAR_ALPHA, NODE_NEAR_ALPHA, point.t) +
                point.motion.brightness,
            ).toFixed(2),
          );
        }
        const halo = haloRefs.current[index];
        if (halo) {
          const glowScale = lerp(GLOW_FAR_SCALE, GLOW_NEAR_SCALE, point.t);
          halo.setAttribute("cx", point.x.toFixed(1));
          halo.setAttribute("cy", point.y.toFixed(1));
          halo.setAttribute(
            "r",
            (lerp(NODE_FAR_R, NODE_NEAR_R, point.t) * glowScale).toFixed(1),
          );
          halo.setAttribute(
            "fill-opacity",
            lerp(GLOW_FAR_ALPHA, GLOW_NEAR_ALPHA, point.t).toFixed(2),
          );
        }
      });

      bucketEdgesByDepth(state.graph.edges, displayed).forEach(
        (pairs, band) => {
          const path = edgeRefs.current[band];
          if (!path) {
            return;
          }
          path.setAttribute("d", segmentPath(pairs, displayed));
          path.setAttribute("stroke-opacity", EDGE_BAND_ALPHA[band].toFixed(2));
          path.setAttribute("stroke-width", EDGE_BAND_WIDTH[band].toFixed(2));
        },
      );

      const dotDepth: number[] = new Array(state.motion.dotPaths.length);
      state.motion.dotPaths.forEach((dot, index) => {
        const frame = state.motion.dotFrame(dot, elapsedMs);
        const from = displayed[frame.fromVertex];
        const to = displayed[frame.toVertex];
        const depth = lerp(from.t, to.t, frame.frac);
        dotDepth[index] = depth;
        const circle = dotRefs.current[index];
        if (!circle) {
          return;
        }
        circle.setAttribute("cx", lerp(from.x, to.x, frame.frac).toFixed(1));
        circle.setAttribute("cy", lerp(from.y, to.y, frame.frac).toFixed(1));
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
        const point = displayed[pulse.vertex];
        if (!ring || !point) {
          return;
        }
        const node = state.nodes[state.vertexToNodeIdx[pulse.vertex]];
        const baseR = lerp(NODE_FAR_R, NODE_NEAR_R, point.t);
        ring.setAttribute("cx", point.x.toFixed(1));
        ring.setAttribute("cy", point.y.toFixed(1));
        ring.setAttribute("r", (baseR * point.motion.ringScale).toFixed(2));
        ring.setAttribute("stroke-width", (baseR * 0.25).toFixed(2));
        ring.setAttribute(
          "stroke",
          node.teal ? "var(--color-cc-success)" : "var(--color-cc-accent)",
        );
        ring.setAttribute(
          "stroke-opacity",
          (
            point.motion.ringAlpha * lerp(DOT_FAR_ALPHA_FACTOR, 1, point.t)
          ).toFixed(2),
        );
        ringDepth[slot] = point.t;
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
          .map((_, index) => index)
          .sort((a, b) => depthOf(refs[a]) - depthOf(refs[b]));
        const previous = paintOrderRef.current;
        let changed = previous.length !== order.length;
        for (let i = 0; !changed && i < order.length; i++) {
          if (previous[i] !== order[i]) {
            changed = true;
          }
        }
        if (changed) {
          for (const index of order) {
            const ref = refs[index];
            const element =
              ref.kind === "node"
                ? groupRefs.current[ref.idx]
                : ref.kind === "dot"
                  ? dotRefs.current[ref.idx]
                  : ringRefs.current[ref.idx];
            if (element) {
              svg.appendChild(element);
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
        <radialGradient id={accentGlowId}>
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
        <radialGradient id={successGlowId}>
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
      {state.edgeGroups.map((group, index) => (
        <path
          key={index}
          ref={(element) => {
            edgeRefs.current[index] = element;
          }}
          d={group.d}
          fill="none"
          stroke="var(--color-cc-accent)"
          strokeOpacity={group.alpha.toFixed(2)}
          strokeWidth={group.width.toFixed(2)}
          strokeLinecap="round"
        />
      ))}
      {state.nodes.map((node, index) => {
        const color = node.teal
          ? "var(--color-cc-success)"
          : "var(--color-cc-accent)";
        const glowScale = lerp(GLOW_FAR_SCALE, GLOW_NEAR_SCALE, node.depth);
        const glowAlpha = lerp(GLOW_FAR_ALPHA, GLOW_NEAR_ALPHA, node.depth);
        return (
          <g
            key={index}
            ref={(element) => {
              groupRefs.current[index] = element;
            }}
          >
            <circle
              ref={(element) => {
                haloRefs.current[index] = element;
              }}
              cx={node.x.toFixed(1)}
              cy={node.y.toFixed(1)}
              r={(node.r * glowScale).toFixed(1)}
              fill={`url(#${node.teal ? successGlowId : accentGlowId})`}
              fillOpacity={glowAlpha.toFixed(2)}
            />
            <circle
              ref={(element) => {
                nodeRefs.current[index] = element;
              }}
              cx={node.x.toFixed(1)}
              cy={node.y.toFixed(1)}
              r={node.r.toFixed(2)}
              fill={color}
              fillOpacity={node.alpha.toFixed(2)}
            />
          </g>
        );
      })}
    </svg>
  );
}
