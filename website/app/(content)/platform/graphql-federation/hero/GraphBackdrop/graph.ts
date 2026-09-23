// The node/edge model for the hero's federated graph: a gateway core at
// the axis, five or six subgraph clusters scattered around a ring, plus a
// sparse dust layer for texture at a third, finer scale. Positions are
// generated once (deterministic PRNG) and rotated live about the Y axis
// in the paint step; nothing here is animated.
import { AMBER, CORAL, CYAN, GREEN, SLATE, TEAL, VIOLET } from "../palette";

function mulberry32(seed: number) {
  let a = seed >>> 0;
  return function rand() {
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export type LayoutMode = "landscape" | "portrait";

export interface GraphNode {
  readonly cluster: number;
  readonly radius: number;
  readonly angle0: number;
  readonly y: number;
  readonly size: number;
  readonly tint: string;
  readonly hot: boolean;
  readonly phase: number;
}

export interface GraphEdge {
  readonly a: number;
  readonly b: number;
  readonly tint: string;
  readonly weight: number;
  readonly phase: number;
}

export interface DustPoint {
  readonly radius: number;
  readonly angle0: number;
  readonly y: number;
  readonly size: number;
  readonly phase: number;
}

export interface GraphModel {
  readonly nodes: readonly GraphNode[];
  readonly edges: readonly GraphEdge[];
  readonly lightEdges: readonly number[];
  readonly dust: readonly DustPoint[];
}

const CLUSTER_TINTS = [CYAN, TEAL, VIOLET, GREEN, AMBER, SLATE];
const CLUSTER_COUNT = 6;
const NODES_PER_CLUSTER = 40;
const GATEWAY_NODES = 5;
const DUST_COUNT = 70;

export function buildGraph(mode: LayoutMode): GraphModel {
  const rand = mulberry32(mode === "portrait" ? 0xf00dc0de : 0x0c0ffee1);
  const nodes: GraphNode[] = [];
  const edges: GraphEdge[] = [];

  // With the camera now centred on the viewport (sceneLayout.ts), the
  // gateway region carries its own downward world-space offset so its
  // coral core lands just below the button row, fully outside the
  // copy-clear zone, instead of dead-centre behind the copy.
  const gatewayY = mode === "portrait" ? -3.3 : -2.0;

  const gatewayStart = nodes.length;
  for (let i = 0; i < GATEWAY_NODES; i++) {
    const isCore = i === 0;
    const r = isCore ? 0 : 0.35 + rand() * 0.5;
    const theta = rand() * Math.PI * 2;
    const y = gatewayY + (isCore ? 0 : (rand() - 0.5) * 0.6);
    nodes.push({
      cluster: -1,
      radius: r,
      angle0: theta,
      y,
      size: isCore ? 2.8 : 1.1,
      tint: CORAL,
      hot: isCore,
      phase: rand() * Math.PI * 2,
    });
  }
  for (let i = gatewayStart + 1; i < gatewayStart + GATEWAY_NODES; i++) {
    edges.push({
      a: gatewayStart,
      b: i,
      tint: CORAL,
      weight: 0.65,
      phase: rand() * Math.PI * 2,
    });
  }

  const clusterRadius = mode === "portrait" ? 2.5 : 3.6;
  const clusterSpread = mode === "portrait" ? 1.5 : 1.15;
  // World-Y bands above and below the (now vertically centred) copy: the
  // camera's near/far perspective swing displaces the upper band more
  // than the lower one for the same world offset, so the two magnitudes
  // are tuned (not identical) to both keep every cluster's centre inside
  // the 5-95% frame-height band across a full rotation and read as
  // symmetric strips above and below the copy.
  const bandBaseUpper = mode === "portrait" ? 1.1 : 0.55;
  const bandBaseLower = mode === "portrait" ? 1.3 : 0.8;
  const bandJitter = mode === "portrait" ? 0.2 : 0.08;
  const entryNodes: number[] = [];

  for (let c = 0; c < CLUSTER_COUNT; c++) {
    const theta0 = (c / CLUSTER_COUNT) * Math.PI * 2 + rand() * 0.15;
    // Clusters alternate above/below the copy band at every width (not
    // only in portrait), so the ring reads as filling the viewport's
    // height instead of hugging the copy's own horizontal band.
    const band = c % 2 === 0 ? -1 : 1;
    const centerY =
      band === 1
        ? bandBaseUpper + rand() * bandJitter // upper band, toward the header
        : -(bandBaseLower + rand() * bandJitter); // lower band, toward the gateway
    const tint = CLUSTER_TINTS[c % CLUSTER_TINTS.length];
    const start = nodes.length;
    for (let i = 0; i < NODES_PER_CLUSTER; i++) {
      const jr = (rand() - 0.5) * clusterSpread;
      const jtheta = (rand() - 0.5) * 0.55;
      const jy = (rand() - 0.5) * clusterSpread;
      const useTint = rand() < 0.16;
      nodes.push({
        cluster: c,
        radius: clusterRadius + jr,
        angle0: theta0 + jtheta,
        y: centerY + jy,
        size: 0.55 + rand() * 0.65,
        tint: useTint ? tint : rand() < 0.5 ? CYAN : SLATE,
        hot: false,
        phase: rand() * Math.PI * 2,
      });
    }
    for (let i = 1; i < NODES_PER_CLUSTER; i++) {
      const links = i < 3 ? 1 : 1 + (rand() < 0.35 ? 1 : 0);
      for (let k = 0; k < links; k++) {
        const back = 1 + Math.floor(rand() * Math.min(i, 5));
        const target = start + Math.max(0, i - back);
        if (target === start + i) {
          continue;
        }
        edges.push({
          a: start + i,
          b: target,
          tint: SLATE,
          weight: 0.3 + rand() * 0.25,
          phase: rand() * Math.PI * 2,
        });
      }
    }
    entryNodes.push(start);
    edges.push({
      a: gatewayStart,
      b: start,
      tint: CYAN,
      weight: 0.5,
      phase: rand() * Math.PI * 2,
    });
  }

  for (let c = 0; c < CLUSTER_COUNT; c++) {
    if (rand() < 0.7) {
      const next = (c + 1) % CLUSTER_COUNT;
      edges.push({
        a: entryNodes[c],
        b: entryNodes[next],
        tint: TEAL,
        weight: 0.35,
        phase: rand() * Math.PI * 2,
      });
    }
  }

  const lightEdges: number[] = [];
  for (let i = 0; i < 3 && edges.length > 0; i++) {
    lightEdges.push(Math.floor(rand() * edges.length));
  }

  const dust: DustPoint[] = [];
  const dustRadiusMax = clusterRadius + clusterSpread + 2.2;
  for (let i = 0; i < DUST_COUNT; i++) {
    dust.push({
      radius: clusterRadius * 0.6 + rand() * dustRadiusMax,
      angle0: rand() * Math.PI * 2,
      y: (rand() - 0.5) * (mode === "portrait" ? 5 : 4.6),
      size: 0.18 + rand() * 0.22,
      phase: rand() * Math.PI * 2,
    });
  }

  return { nodes, edges, lightEdges, dust };
}
