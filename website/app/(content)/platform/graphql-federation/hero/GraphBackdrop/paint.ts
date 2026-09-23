// The per-frame draw: rotate the graph about the vertical axis, project it
// through the camera, depth-sort (edges, then nodes, back to front), paint
// glow on the near nodes only, a soft travelling light on a few edges, the
// copy-clear scrim and the outer vignette.
import { project, ringPoint, type Camera } from "./camera";
import { rgba, WHITE } from "./color";
import type { DustPoint, GraphModel, GraphNode } from "./graph";
import { NAVY, SLATE } from "../palette";

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
  readonly time: number;
  readonly copyRect: Rect | null;
}

const OMEGA = (Math.PI * 2) / 105; // one revolution in 105s
const BREATHE_FREQ = (Math.PI * 2) / 7.4;
const SHIMMER_FREQ = (Math.PI * 2) / 5.2;
const LIGHT_PERIOD = 15.5;
const SIZE_K = 0.023;
const MIN_SCALE_F = 0.45;
const MAX_SCALE_F = 2.4;
const GLOW_FRACTION = 0.17;

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

function projectNode(n: GraphNode, theta: number, cam: Camera): Projected {
  const p = ringPoint(n.radius, n.angle0 + theta, n.y);
  return project(p, cam);
}

function projectDust(n: DustPoint, theta: number, cam: Camera): Projected {
  const p = ringPoint(n.radius, n.angle0 + theta, n.y);
  return project(p, cam);
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
  return smoothstep(0.75, 1.35, Math.max(dx, dy));
}

function paintCopyScrim(ctx: CanvasRenderingContext2D, rect: Rect) {
  const pad = 24;
  const x = rect.x - pad;
  const y = rect.y - pad;
  const w = rect.width + pad * 2;
  const h = rect.height + pad * 2;
  const cx = x + w / 2;
  const cy = y + h / 2;
  // A soft ellipse hugging the padded rect's own aspect, fading out over
  // ~125px beyond it, instead of a circle sized off the rect's diagonal
  // (which used to balloon out to the full viewport height on a wide,
  // short copy block).
  const extend = 125;
  const rx = w / 2 + extend;
  const ry = h / 2 + extend;
  const innerFrac = (w / 2 / rx + h / 2 / ry) / 2;
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(rx, ry);
  const grad = ctx.createRadialGradient(0, 0, innerFrac, 0, 0, 1);
  grad.addColorStop(0, rgba(NAVY, 0.8));
  grad.addColorStop(1, rgba(NAVY, 0));
  ctx.fillStyle = grad;
  ctx.beginPath();
  ctx.arc(0, 0, 1, 0, Math.PI * 2);
  ctx.fill();
  ctx.restore();
  ctx.fillStyle = rgba(NAVY, 0.8);
  ctx.fillRect(x, y, w, h);
}

function paintVignette(ctx: CanvasRenderingContext2D, w: number, h: number) {
  const cx = w / 2;
  const cy = h * 0.44;
  const r = Math.hypot(w, h) * 0.62;
  const g = ctx.createRadialGradient(cx, cy, r * 0.5, cx, cy, r);
  g.addColorStop(0, rgba(NAVY, 0));
  g.addColorStop(1, rgba(NAVY, 0.55));
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, w, h);
}

export function paint({
  ctx,
  w,
  h,
  camera,
  graph,
  time,
  copyRect,
}: PaintOptions) {
  ctx.clearRect(0, 0, w, h);
  const theta = time * OMEGA;
  const baseScale = camera.baseScale;

  // Fine-scale dust layer, drawn first and dimmest (the third scale of
  // detail beyond the clusters and the individual nodes/edges).
  ctx.globalCompositeOperation = "source-over";
  for (const d of graph.dust) {
    const pr = projectDust(d, theta, camera);
    if (pr.depth <= 1) {
      continue;
    }
    const nf = Math.max(
      MIN_SCALE_F,
      Math.min(MAX_SCALE_F, pr.scale / baseScale),
    );
    const fall = copyFalloff(pr.x, pr.y, copyRect);
    const alpha =
      0.16 * nf * fall * (0.7 + 0.3 * Math.sin(time * BREATHE_FREQ + d.phase));
    if (alpha <= 0.008) {
      continue;
    }
    const r = Math.max(0.35, d.size * pr.scale * SIZE_K);
    ctx.fillStyle = rgba(SLATE, alpha);
    ctx.beginPath();
    ctx.arc(pr.x, pr.y, r, 0, Math.PI * 2);
    ctx.fill();
  }

  const nodeProj = graph.nodes.map((n) => projectNode(n, theta, camera));
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
    const nf = Math.max(
      MIN_SCALE_F,
      Math.min(MAX_SCALE_F, (a.scale + b.scale) / 2 / baseScale),
    );
    const fallA = copyFalloff(a.x, a.y, copyRect);
    const fallB = copyFalloff(b.x, b.y, copyRect);
    const shimmer = 0.82 + 0.18 * Math.sin(time * SHIMMER_FREQ + e.phase);
    const alpha = e.weight * 0.78 * nf * Math.min(fallA, fallB) * shimmer;
    if (alpha <= 0.01) {
      continue;
    }
    ctx.strokeStyle = rgba(e.tint, alpha);
    ctx.lineWidth = Math.max(0.5, 0.7 * nf);
    ctx.beginPath();
    ctx.moveTo(a.x, a.y);
    ctx.lineTo(b.x, b.y);
    ctx.stroke();
  }

  const nodeOrder = graph.nodes
    .map((n, i) => ({ i, depth: nodeProj[i].depth, scale: nodeProj[i].scale }))
    .sort((x, y) => y.depth - x.depth);
  const glowCount = Math.ceil(nodeOrder.length * GLOW_FRACTION);
  const glowThreshold = [...nodeOrder]
    .sort((x, y) => y.scale - x.scale)
    .slice(0, glowCount)
    .reduce((min, n) => Math.min(min, n.scale), Infinity);

  ctx.globalCompositeOperation = "lighter";
  for (const { i } of nodeOrder) {
    const n = graph.nodes[i];
    const pr = nodeProj[i];
    if (pr.depth <= 1) {
      continue;
    }
    const nf = Math.max(
      MIN_SCALE_F,
      Math.min(MAX_SCALE_F, pr.scale / baseScale),
    );
    const fall = copyFalloff(pr.x, pr.y, copyRect);
    if (fall <= 0.02) {
      continue;
    }
    const breathe = 1 + 0.14 * Math.sin(time * BREATHE_FREQ + n.phase);
    const baseAlpha = (n.hot ? 0.95 : 0.8) * nf * fall * breathe;
    const r = Math.max(0.9, n.size * pr.scale * SIZE_K);
    const isNear = pr.scale >= glowThreshold;
    if (isNear || n.hot) {
      const glowR = r * (n.hot ? 9 : 3.4);
      const glow = ctx.createRadialGradient(pr.x, pr.y, 0, pr.x, pr.y, glowR);
      glow.addColorStop(0, rgba(n.tint, baseAlpha * (n.hot ? 0.55 : 0.5)));
      glow.addColorStop(1, rgba(n.tint, 0));
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(pr.x, pr.y, glowR, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.fillStyle = rgba(n.tint, Math.min(1, baseAlpha));
    ctx.beginPath();
    ctx.arc(pr.x, pr.y, r, 0, Math.PI * 2);
    ctx.fill();
    if (n.hot) {
      ctx.fillStyle = rgba(WHITE, Math.min(1, baseAlpha * 0.85));
      ctx.beginPath();
      ctx.arc(pr.x, pr.y, r * 0.42, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  for (const idx of graph.lightEdges) {
    const e = graph.edges[idx];
    if (!e) {
      continue;
    }
    const a = nodeProj[e.a];
    const b = nodeProj[e.b];
    if (a.depth <= 1 || b.depth <= 1) {
      continue;
    }
    const u = (((time / LIGHT_PERIOD + idx * 0.37) % 1) + 1) % 1;
    const fade = Math.min(smoothstep(0, 0.12, u), 1 - smoothstep(0.88, 1, u));
    if (fade <= 0.01) {
      continue;
    }
    const x = a.x + (b.x - a.x) * u;
    const y = a.y + (b.y - a.y) * u;
    const fall = copyFalloff(x, y, copyRect);
    const rr = 22;
    const g = ctx.createRadialGradient(x, y, 0, x, y, rr);
    g.addColorStop(0, rgba(e.tint, 0.42 * fade * fall));
    g.addColorStop(1, rgba(e.tint, 0));
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.arc(x, y, rr, 0, Math.PI * 2);
    ctx.fill();
  }

  ctx.globalCompositeOperation = "source-over";
  if (copyRect) {
    paintCopyScrim(ctx, copyRect);
  }
  paintVignette(ctx, w, h);
}
