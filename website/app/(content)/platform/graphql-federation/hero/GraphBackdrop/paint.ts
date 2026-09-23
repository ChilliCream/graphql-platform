// The one-shot draw: a sparse constellation of clean discs and thin lines,
// a vignette and a soft scrim behind the copy. Nothing here reads a clock
// -- this is a still frame, painted once on mount and once per resize,
// never from a loop. Node/edge size, alpha and colour are all resolved by
// graph.ts; this file only draws them.
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

const HUB_HALO_R = 14;
const TINTS = [CYAN, TEAL] as const;

/**
 * A soft scrim hugging the copy rect -- not the whole viewport -- so at a
 * narrow, tall viewport it covers only the copy band and the graph keeps
 * its near alpha everywhere else (the portrait fix). Its reach is a
 * fraction of the rect's own size plus a small fixed pad, instead of one
 * fixed pad that overwhelms a short, narrow rect on desktop or undershoots
 * a tall one on mobile.
 */
function paintCopyScrim(ctx: CanvasRenderingContext2D, rect: Rect) {
  const pad = 24;
  const x = rect.x - pad;
  const y = rect.y - pad;
  const w = rect.width + pad * 2;
  const h = rect.height + pad * 2;
  const cx = x + w / 2;
  const cy = y + h / 2;
  const extendX = Math.min(90, w * 0.22);
  const extendY = Math.min(90, h * 0.22);
  const rx = w / 2 + extendX;
  const ry = h / 2 + extendY;
  const cornerFrac = Math.hypot(w / 2 / rx, h / 2 / ry);
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(rx, ry);
  const grad = ctx.createRadialGradient(0, 0, cornerFrac, 0, 0, 1);
  grad.addColorStop(0, rgba(NAVY, 0.55));
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

  // Edges first, under every node. Near edges outside the copy zone get a
  // white mix on top of the usual cyan tint, keeping the near band's
  // rendered luminance clearly above the "lines visible" floor even after
  // a thin, antialiased 1px stroke dilutes its own per-pixel colour below
  // the nominal alpha composite -- far edges keep the plain tint (so depth
  // fade still reads), and copy-zone edges never get it (so it can't erode
  // their contrast).
  ctx.lineCap = "round";
  for (const e of graph.edges) {
    ctx.lineWidth = e.lineWidth;
    const lightenNear = e.near && e.darken === 0;
    ctx.strokeStyle = rgba(
      SLATE,
      e.alpha,
      lightenNear
        ? [
            { with: CYAN, ratio: 0.4 },
            { with: WHITE, ratio: 0.72 },
          ]
        : { with: CYAN, ratio: 0.4 },
      e.darken,
    );
    ctx.beginPath();
    ctx.moveTo(e.points[0].x, e.points[0].y);
    for (let k = 1; k < e.points.length; k++) {
      ctx.lineTo(e.points[k].x, e.points[k].y);
    }
    ctx.stroke();
  }

  const nodeOrder = graph.nodes
    .map((n, i) => ({ i, alpha: n.alpha }))
    .sort((a, b) => a.alpha - b.alpha);

  for (const { i } of nodeOrder) {
    const n = graph.nodes[i];
    const tint = TINTS[n.tint];

    if (n.hub) {
      const glow = ctx.createRadialGradient(n.x, n.y, 0, n.x, n.y, HUB_HALO_R);
      glow.addColorStop(0, rgba(CYAN, 0.28));
      glow.addColorStop(1, rgba(CYAN, 0));
      ctx.fillStyle = glow;
      ctx.beginPath();
      ctx.arc(n.x, n.y, HUB_HALO_R, 0, Math.PI * 2);
      ctx.fill();
    }

    ctx.fillStyle = rgba(tint, n.alpha, undefined, n.darken);
    ctx.beginPath();
    ctx.arc(n.x, n.y, n.r, 0, Math.PI * 2);
    ctx.fill();

    if (n.hub) {
      ctx.fillStyle = rgba(WHITE, 0.9);
      ctx.beginPath();
      ctx.arc(n.x, n.y, n.r * 0.42, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  if (copyRect) {
    paintCopyScrim(ctx, copyRect);
  }
  paintVignette(ctx, w, h);
}
