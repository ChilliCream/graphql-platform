// The one-shot draw: a sparse constellation of clean discs and thin lines,
// a vignette and a soft scrim behind the copy. Nothing here reads a clock
// -- this is a still frame, painted once on mount and once per resize,
// never from a loop. No dust layer, no blur, no glow beyond each cluster's
// one hub halo: restraint is the point.
import type { GraphModel } from "./graph";
import { rgba, WHITE } from "./color";
import { CYAN, NAVY, SLATE, TEAL } from "../palette";

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
  readonly graph: GraphModel;
  readonly copyRect: Rect | null;
}

const FAR_R = 2;
const FAR_R_SPAN = 1;
const NEAR_R = 4;
const NEAR_R_SPAN = 2;
const HUB_HALO_R = 14;

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
  const cornerFrac = Math.hypot(w / 2 / rx, h / 2 / ry);
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(rx, ry);
  const grad = ctx.createRadialGradient(0, 0, cornerFrac, 0, 0, 1);
  grad.addColorStop(0, rgba(NAVY, 0.7));
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
  const g = ctx.createRadialGradient(cx, cy, r * 0.5, cx, cy, r);
  g.addColorStop(0, rgba(NAVY, 0));
  g.addColorStop(1, rgba(NAVY, 0.55));
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, w, h);
}

export function paint({ ctx, w, h, graph, copyRect }: PaintOptions) {
  ctx.clearRect(0, 0, w, h);

  // Edges first, under every node.
  ctx.lineCap = "round";
  ctx.lineWidth = 1;
  for (const e of graph.edges) {
    ctx.strokeStyle = rgba(SLATE, e.alpha, { with: CYAN, ratio: 0.4 });
    ctx.beginPath();
    ctx.moveTo(e.points[0].x, e.points[0].y);
    for (let k = 1; k < e.points.length; k++) {
      ctx.lineTo(e.points[k].x, e.points[k].y);
    }
    ctx.stroke();
  }

  const nodeOrder = graph.nodes
    .map((n, i) => ({ i, depth: n.depth }))
    .sort((a, b) => a.depth - b.depth);

  for (const { i } of nodeOrder) {
    const n = graph.nodes[i];
    const tint = i % 2 === 0 ? CYAN : TEAL;
    const r = n.hub
      ? NEAR_R + NEAR_R_SPAN
      : n.depth > 0.5
        ? NEAR_R + NEAR_R_SPAN * (n.depth - 0.5) * 2
        : FAR_R + FAR_R_SPAN * n.depth * 2;
    const alpha = n.hub
      ? 1
      : n.depth > 0.5
        ? 0.8 + 0.2 * (n.depth - 0.5) * 2
        : 0.35 + 0.15 * n.depth * 2;

    if (n.hub) {
      const glow = ctx.createRadialGradient(n.x, n.y, 0, n.x, n.y, HUB_HALO_R);
      glow.addColorStop(0, rgba(CYAN, 0.28));
      glow.addColorStop(1, rgba(CYAN, 0));
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(n.x, n.y, HUB_HALO_R, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.fillStyle = rgba(tint, alpha);
    ctx.beginPath();
    ctx.arc(n.x, n.y, r, 0, Math.PI * 2);
    ctx.fill();

    if (n.hub) {
      ctx.fillStyle = rgba(WHITE, 0.9);
      ctx.beginPath();
      ctx.arc(n.x, n.y, r * 0.42, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  if (copyRect) {
    paintCopyScrim(ctx, copyRect);
  }
  paintVignette(ctx, w, h);
}
