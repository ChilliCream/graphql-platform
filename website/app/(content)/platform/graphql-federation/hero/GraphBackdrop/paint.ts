// The one-shot draw: project the static graph through the camera, depth-
// sort (edges, then nodes, back to front), paint a soft depth-of-field blur
// on the far layer, a restrained glow on the near nodes and the gateway's
// coral core, a soft radial scrim behind the copy and an outer vignette.
// Nothing here reads a clock -- this is a still frame, called once on mount
// and once per resize, never from a loop.
import { project, ringPoint, type Camera } from "./camera";
import { rgba, WHITE } from "./color";
import type { DustPoint, GraphModel } from "./graph";
import { CYAN, NAVY, SLATE } from "../palette";

export interface Rect {
  readonly x: number;
  readonly y: number;
  readonly width: number;
  readonly height: number;
}

export interface PaintOptions {
  readonly ctx: CanvasRenderingContext2D;
  readonly w: number;
  readonly h: number;
  readonly camera: Camera;
  readonly graph: GraphModel;
  readonly copyRect: Rect | null;
}

const MIN_SCALE_F = 0.45;
const MAX_SCALE_F = 2.4;
const FAR_R = 1.1;
const NEAR_R = 6.5;
const HOT_R = 7;
const TIER_MULT = [0.6, 0.85, 1.15] as const;

function clamp01(v: number) {
  return v < 0 ? 0 : v > 1 ? 1 : v;
}

function smoothstep(a: number, b: number, x: number) {
  const t = clamp01((x - a) / (b - a));
  return t * t * (3 - 2 * t);
}

interface Projected {
  readonly x: number;
  readonly y: number;
  readonly scale: number;
  readonly depth: number;
}

function project3(radius: number, angle: number, y: number, cam: Camera) {
  return project(ringPoint(radius, angle, y), cam);
}

/** 0 (farthest allowed) .. 1 (nearest allowed) depth read for a projection. */
function depthFactor(pr: Projected, baseScale: number) {
  const nf = clamp01(
    (Math.max(MIN_SCALE_F, Math.min(MAX_SCALE_F, pr.scale / baseScale)) -
      MIN_SCALE_F) /
      (MAX_SCALE_F - MIN_SCALE_F),
  );
  return nf;
}

function copyFalloff(x: number, y: number, rect: Rect | null): number {
  if (!rect) {
    return 1;
  }
  const cx = rect.x + rect.width / 2;
  const cy = rect.y + rect.height / 2;
  const halfW = rect.width / 2 + 24;
  const halfH = rect.height / 2 + 24;
  const dx = Math.abs(x - cx) / halfW;
  const dy = Math.abs(y - cy) / halfH;
  return smoothstep(0.85, 1.2, Math.max(dx, dy));
}

function paintCopyScrim(ctx: CanvasRenderingContext2D, rect: Rect) {
  const pad = 24;
  const x = rect.x - pad;
  const y = rect.y - pad;
  const w = rect.width + pad * 2;
  const h = rect.height + pad * 2;
  const cx = x + w / 2;
  const cy = y + h / 2;
  const extend = 150;
  const rx = w / 2 + extend;
  const ry = h / 2 + extend;
  // The flat, fully-opaque zone must cover the padded rect's own corners,
  // not just its edge midpoints, so it is sized off the corner's distance
  // in the gradient's local (scaled) space -- never a hard rectangle on
  // top, only a soft radial fade the whole way out.
  const cornerFrac = Math.hypot(w / 2 / rx, h / 2 / ry);
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(rx, ry);
  const grad = ctx.createRadialGradient(0, 0, cornerFrac, 0, 0, 1);
  grad.addColorStop(0, rgba(NAVY, 0.82));
  grad.addColorStop(1, rgba(NAVY, 0));
  ctx.fillStyle = grad;
  ctx.beginPath();
  ctx.arc(0, 0, 1, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
}

function paintVignette(ctx: CanvasRenderingContext2D, w: number, h: number) {
  const cx = w / 2;
  const cy = h / 2;
  const r = Math.hypot(w, h) * 0.62;
  const g = ctx.createRadialGradient(cx, cy, r * 0.48, cx, cy, r);
  g.addColorStop(0, rgba(NAVY, 0));
  g.addColorStop(1, rgba(NAVY, 0.58));
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, w, h);
}

export function paint({ ctx, w, h, camera, graph, copyRect }: PaintOptions) {
  ctx.clearRect(0, 0, w, h);
  const baseScale = camera.baseScale;

  // Dust: the finest, farthest texture layer, drawn first, dimmest and
  // with a gentle blur standing in for depth-of-field softness.
  ctx.globalCompositeOperation = "source-over";
  const dustBuckets: [DustPoint[], DustPoint[]] = [[], []];
  for (const d of graph.dust) {
    const pr = project3(d.radius, d.angle, d.y, camera);
    if (pr.depth <= 1) {
      continue;
    }
    const df = depthFactor(pr, baseScale);
    dustBuckets[df > 0.55 ? 1 : 0].push(d);
  }
  for (const [bucketIdx, points] of dustBuckets.entries()) {
    ctx.filter = bucketIdx === 0 ? "blur(1.1px)" : "blur(0.4px)";
    for (const d of points) {
      const pr = project3(d.radius, d.angle, d.y, camera);
      const df = depthFactor(pr, baseScale);
      const fall = copyFalloff(pr.x, pr.y, copyRect);
      const alpha = 0.14 * (0.35 + 0.65 * df) * fall;
      if (alpha <= 0.006) {
        continue;
      }
      const r = FAR_R * 0.55 * d.size * (0.6 + 0.5 * df);
      ctx.fillStyle = rgba(SLATE, alpha, { with: d.tint, ratio: d.tintRatio });
      ctx.beginPath();
      ctx.arc(pr.x, pr.y, Math.max(0.3, r), 0, Math.PI * 2);
      ctx.fill();
    }
  }
  ctx.filter = "none";

  const nodeProj = graph.nodes.map((n) =>
    project3(n.radius, n.angle, n.y, camera),
  );

  const edgeOrder = graph.edges
    .map((e, i) => ({
      i,
      depth: (nodeProj[e.a].depth + nodeProj[e.b].depth) / 2,
    }))
    .sort((a, b) => b.depth - a.depth);

  ctx.lineCap = "round";
  for (const { i } of edgeOrder) {
    const e = graph.edges[i];
    const a = nodeProj[e.a];
    const b = nodeProj[e.b];
    if (a.depth <= 1 || b.depth <= 1) {
      continue;
    }
    const dfA = depthFactor(a, baseScale);
    const dfB = depthFactor(b, baseScale);
    const df = (dfA + dfB) / 2;
    const fallA = copyFalloff(a.x, a.y, copyRect);
    const fallB = copyFalloff(b.x, b.y, copyRect);
    const alpha = e.weight * 0.5 * (0.3 + 0.7 * df) * Math.min(fallA, fallB);
    if (alpha <= 0.008) {
      continue;
    }
    ctx.strokeStyle = rgba(e.tint, alpha);
    ctx.lineWidth = Math.max(0.4, 0.55 * (0.4 + 0.6 * df));
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
  }

  const nodeOrder = graph.nodes
    .map((n, i) => ({ i, depth: nodeProj[i].depth }))
    .sort((x, y) => y.depth - x.depth);

  ctx.globalCompositeOperation = "lighter";
  for (const { i } of nodeOrder) {
    const n = graph.nodes[i];
    const pr = nodeProj[i];
    if (pr.depth <= 1) {
      continue;
    }
    // The gateway core is the scene's one deliberate hot point: it always
    // reads at strong presence, not subject to the same depth roll-off as
    // the decorative cluster nodes (which is what a literal depth read
    // would otherwise do to a node placed a little further from camera).
    const rawDf = depthFactor(pr, baseScale);
    const df = n.hot ? Math.max(0.85, rawDf) : rawDf;
    const fall = copyFalloff(pr.x, pr.y, copyRect);
    if (fall <= 0.02) {
      continue;
    }
    // A low floor here (rather than a shallow 0.4-1.0 ramp) is what gives
    // the depth read its contrast: the farthest nodes should read as
    // genuinely dim, not just a little dimmer than the nearest ones.
    const baseAlpha = (n.hot ? 0.95 : 0.62) * (0.16 + 0.84 * df) * fall;
    const tierMult = TIER_MULT[n.tier];
    const baseR = FAR_R + (NEAR_R - FAR_R) * df;
    const r = n.hot
      ? Math.min(HOT_R, baseR * 1.05)
      : Math.min(NEAR_R, baseR * tierMult);
    const isNear = df > 0.62;
    if (isNear || n.hot) {
      const glowR = r * (n.hot ? 6.5 : 3.2);
      const glow = ctx.createRadialGradient(pr.x, pr.y, 0, pr.x, pr.y, glowR);
      const glowAlpha = baseAlpha * (n.hot ? 0.32 : 0.16);
      glow.addColorStop(
        0,
        rgba(n.tint, glowAlpha, {
          with: SLATE,
          ratio: n.hot ? 0 : 1 - n.tintRatio,
        }),
      );
      glow.addColorStop(1, rgba(n.tint, 0));
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(pr.x, pr.y, glowR, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.fillStyle = rgba(SLATE, Math.min(1, baseAlpha), {
      with: n.tint,
      ratio: n.tintRatio,
    });
    ctx.beginPath();
    ctx.arc(pr.x, pr.y, r, 0, Math.PI * 2);
    ctx.fill();
    if (n.hot) {
      ctx.fillStyle = rgba(WHITE, Math.min(1, baseAlpha * 0.8));
      ctx.beginPath();
      ctx.arc(pr.x, pr.y, r * 0.4, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  // A soft, low-alpha cyan wash reads as "one dominant light" behind the
  // busiest part of the scene without drawing anything shaped like a node.
  ctx.globalCompositeOperation = "screen";
  const ambient = ctx.createRadialGradient(
    w / 2,
    h * 0.42,
    0,
    w / 2,
    h * 0.42,
    Math.max(w, h) * 0.55,
  );
  ambient.addColorStop(0, rgba(CYAN, 0.05));
  ambient.addColorStop(1, rgba(CYAN, 0));
  ctx.fillStyle = ambient;
  ctx.fillRect(0, 0, w, h);

  ctx.globalCompositeOperation = "source-over";
  if (copyRect) {
    paintCopyScrim(ctx, copyRect);
  }
  paintVignette(ctx, w, h);
}
