// The node/edge model for the hero's federated graph: a coral gateway core
// at the axis, six subgraph clusters hand-placed around it for an even,
// centre-clear composition, plus a fine dust layer for background texture.
// Everything here is a fixed world-space position generated once from a
// seeded PRNG -- there is no live angle, no per-frame offset, nothing that
// changes after layout. The single frame paint.ts draws is this model,
// projected once.
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

/** Three coarse size classes, for the fine-texture "several scales" read. */
export type Tier = 0 | 1 | 2;

export interface GraphNode {
  readonly cluster: number;
  readonly radius: number;
  readonly angle: number;
  readonly y: number;
  readonly tier: Tier;
  readonly tint: string;
  readonly tintRatio: number;
  readonly hot: boolean;
}

export interface GraphEdge {
  readonly a: number;
  readonly b: number;
  readonly tint: string;
  readonly weight: number;
}

export interface DustPoint {
  readonly radius: number;
  readonly angle: number;
  readonly y: number;
  readonly size: number;
  readonly tint: string;
  readonly tintRatio: number;
}

export interface GraphModel {
  readonly nodes: readonly GraphNode[];
  readonly edges: readonly GraphEdge[];
  readonly dust: readonly DustPoint[];
}

const SERVICE_TINTS = [TEAL, VIOLET, GREEN, AMBER];
const NODES_PER_CLUSTER = 78;
const GATEWAY_SATELLITES = 6;
const DUST_COUNT = 240;

const deg = (d: number) => (d * Math.PI) / 180;

interface ClusterDef {
  readonly angleDeg: number;
  readonly y: number;
  readonly tint: string;
}

// Six clusters, hand-placed by angle and world-y band so the *static* frame
// (no rotation ever mixes the composition) reads as evenly weighted around
// the centred copy: two left, two right, one upper-centre, one lower-centre,
// none crowding the very top (the header) or the very bottom edge.
//
// Landscape has ample horizontal frustum room (the camera's focal length is
// derived from the wider dimension), so clusters spread across a full left/
// right arc. A narrow portrait viewport does not: the horizontal frustum at
// e.g. 375x812 is far tighter than at 768x1024, so a cluster placed near
// the left/right cardinal angles (cos close to +-1) projects off-canvas.
// Portrait therefore keeps every cluster's angle close to the vertical
// (90/270) axis, where cos stays small, and gets its above/below spread
// from the y band instead -- which a tall viewport has plenty of.
function clusterDefs(mode: LayoutMode): readonly ClusterDef[] {
  if (mode === "portrait") {
    // Positive world-y projects toward the header (screen-up), negative
    // toward the gateway (screen-down) -- the same convention the gateway's
    // own `gatewayY` below relies on to clear the button row. Every angle
    // here sits close to the 270 axis (large negative sin, i.e. close to
    // camera), which keeps depth small and projected scale high, so a
    // modest world-y offset still throws far enough up-screen to clear a
    // narrow phone's tight header-to-copy gap -- an angle near the 90 axis
    // (far from camera, low scale) would need a world-y offset large
    // enough to risk landing back inside the copy band instead.
    const upperY = 1.55;
    const lowerY = 1.9;
    return [
      { angleDeg: 260, y: upperY, tint: SERVICE_TINTS[0] },
      { angleDeg: 280, y: upperY * 0.85, tint: SERVICE_TINTS[1] },
      { angleDeg: 245, y: upperY * 1.15, tint: SERVICE_TINTS[2] },
      { angleDeg: 255, y: -lowerY, tint: SERVICE_TINTS[3] },
      { angleDeg: 295, y: -lowerY * 0.85, tint: SERVICE_TINTS[0] },
      { angleDeg: 285, y: -lowerY * 1.15, tint: SERVICE_TINTS[2] },
    ];
  }
  const upperY = 0.72;
  const lowerY = 0.85;
  return [
    { angleDeg: 205, y: -upperY * 0.7, tint: SERVICE_TINTS[0] },
    { angleDeg: 335, y: -upperY, tint: SERVICE_TINTS[1] },
    { angleDeg: 115, y: upperY, tint: SERVICE_TINTS[2] },
    { angleDeg: 65, y: lowerY, tint: SERVICE_TINTS[3] },
    { angleDeg: 250, y: lowerY * 0.75, tint: SERVICE_TINTS[0] },
    { angleDeg: 292, y: -lowerY * 0.55, tint: SERVICE_TINTS[2] },
  ];
}

function pickTier(rand: () => number): Tier {
  const r = rand();
  if (r < 0.12) {
    return 2; // large, rare
  }
  if (r < 0.42) {
    return 1; // medium
  }
  return 0; // small, common -- the fine texture
}

/**
 * `aspect` is the viewport's own width/height, used only to keep portrait
 * clusters inside the narrower horizontal frustum a tall-but-narrow phone
 * has (a tablet in portrait has much more room than a phone does).
 */
export function buildGraph(mode: LayoutMode, aspect: number): GraphModel {
  const rand = mulberry32(mode === "portrait" ? 0xf00dc0de : 0x0c0ffee1);
  const nodes: GraphNode[] = [];
  const edges: GraphEdge[] = [];

  // Gateway: a single coral hot core plus a few close satellites, offset
  // below the copy block so it sits in clear space at full strength.
  const gatewayY = mode === "portrait" ? -3.1 : -1.9;
  const gatewayStart = nodes.length;
  nodes.push({
    cluster: -1,
    radius: 0,
    angle: 0,
    y: gatewayY,
    tier: 2,
    tint: CORAL,
    tintRatio: 1,
    hot: true,
  });
  for (let i = 0; i < GATEWAY_SATELLITES; i++) {
    const theta = (i / GATEWAY_SATELLITES) * Math.PI * 2 + rand() * 0.3;
    nodes.push({
      cluster: -1,
      radius: 0.4 + rand() * 0.35,
      angle: theta,
      y: gatewayY + (rand() - 0.5) * 0.5,
      tier: 0,
      tint: CORAL,
      tintRatio: 0.3,
      hot: false,
    });
    edges.push({
      a: gatewayStart,
      b: gatewayStart + 1 + i,
      tint: SLATE,
      weight: 0.3,
    });
  }

  const safeAspect = Math.max(0.3, Math.min(1, aspect));
  const clusterRadius =
    mode === "portrait" ? Math.min(2.5, 0.5 + safeAspect * 2.6) : 3.5;
  // Portrait's upper/lower clusters sit close to the camera (see
  // clusterDefs above) so their projected scale is high -- the same world-
  // space jitter that reads as a healthy scatter in landscape would throw
  // nodes wildly across the screen there, some back into the copy's own
  // falloff and some past the frame edge. Portrait's spread and angular
  // jitter are both scaled down to keep the cluster visually coherent.
  const clusterSpread =
    mode === "portrait" ? Math.min(0.6, 0.2 + safeAspect * 0.6) : 1.05;
  const angleJitter = mode === "portrait" ? 0.24 : 0.6;
  // The radius jitter is kept separate from clusterSpread for portrait: at
  // an angle close to the 270 axis (see clusterDefs above), radius jitter
  // is almost entirely a depth (world-z) change, not a screen x/y one --
  // cos(angle) is small there, so it barely moves the node sideways. A
  // wider radius jitter than the x/y spread therefore buys real near/far
  // depth contrast without reintroducing the screen scatter clusterSpread
  // was tightened to avoid.
  const radiusJitter = mode === "portrait" ? 3.0 : clusterSpread;
  const entryNodes: number[] = [];

  for (const def of clusterDefs(mode)) {
    const theta0 = deg(def.angleDeg);
    const start = nodes.length;
    for (let i = 0; i < NODES_PER_CLUSTER; i++) {
      const jr = (rand() - 0.5) * radiusJitter;
      const jtheta = (rand() - 0.5) * angleJitter;
      const jy = (rand() - 0.5) * clusterSpread;
      const roll = rand();
      // Restraint: mostly plain slate structure, a good share of faint
      // cyan (the one dominant light), and only a few nodes carrying a
      // faint service tint -- never a saturated disc.
      const tint = roll < 0.62 ? SLATE : roll < 0.88 ? CYAN : def.tint;
      const tintRatio = roll < 0.62 ? 0 : roll < 0.88 ? 0.55 : 0.32;
      nodes.push({
        cluster: 0,
        radius: clusterRadius + jr,
        angle: theta0 + jtheta,
        y: def.y + jy,
        tier: pickTier(rand),
        tint,
        tintRatio,
        hot: false,
      });
    }
    for (let i = 1; i < NODES_PER_CLUSTER; i++) {
      const links = i < 3 ? 1 : 1 + (rand() < 0.3 ? 1 : 0);
      for (let k = 0; k < links; k++) {
        const back = 1 + Math.floor(rand() * Math.min(i, 4));
        const target = start + Math.max(0, i - back);
        if (target === start + i) {
          continue;
        }
        edges.push({
          a: start + i,
          b: target,
          tint: rand() < 0.12 ? CYAN : SLATE,
          weight: 0.16 + rand() * 0.16,
        });
      }
    }
    entryNodes.push(start);
  }

  // A few longer, dimmer inter-cluster edges only -- never a bright spoke
  // back to the gateway, and never more than a handful of long lines.
  const backboneLinks: readonly [number, number][] = [
    [0, 2],
    [1, 3],
    [4, 5],
  ];
  for (const [ca, cb] of backboneLinks) {
    edges.push({
      a: entryNodes[ca],
      b: entryNodes[cb],
      tint: SLATE,
      weight: 0.1,
    });
  }

  const dust: DustPoint[] = [];
  const dustRadiusMax = clusterRadius + clusterSpread + 2.6;
  for (let i = 0; i < DUST_COUNT; i++) {
    const cyanDust = rand() < 0.18;
    dust.push({
      radius: clusterRadius * 0.3 + rand() * dustRadiusMax,
      angle: rand() * Math.PI * 2,
      y: (rand() - 0.5) * (mode === "portrait" ? 5.4 : 4.8),
      size: 0.5 + rand() * 0.7,
      tint: cyanDust ? CYAN : SLATE,
      tintRatio: cyanDust ? 0.5 : 0,
    });
  }

  return { nodes, edges, dust };
}
