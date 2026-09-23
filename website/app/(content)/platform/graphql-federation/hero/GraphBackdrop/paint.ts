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
  readonly paragraphRect: Rect | null;
  readonly ctaRect: Rect | null;
}

const HUB_HALO_R = 14;
const TINTS = [CYAN, TEAL] as const;
// An extra scrim behind the paragraph block and the button row, each on
// top of the general copy scrim: two or more independently alpha-capped
// nodes/edges can still converge on the same glyph row and composite
// brighter than a single capped layer alone, which is what pushed the
// paragraph -- and, under this hero's own graph, the button row too --
// under 7:1 even at the copy zone's 0.08/0.35 cap. Each rect gets its own
// tightly feathered scrim (never a visible box or edge), and nothing
// changes anywhere else in the copy block (h1's own rows keep clearing
// 7:1 on the base tier alone).
const EXTRA_SCRIM_ALPHA = 0.82;
const EXTRA_SCRIM_FEATHER = 56;
// See paintExtraScrim/paintCopyScrim: the floor that keeps their radial
// gradient's inner stop below the outer one for any rect aspect ratio.
const CORNER_SAFE_K = 0.7;

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
  const halfW = w / 2;
  const halfH = h / 2;
  // See paintExtraScrim: independent per-axis extends can push
  // hypot(halfW/rx, halfH/ry) at or past 1 for some aspect ratios, which
  // makes the gradient's inner stop undefined; the CORNER_SAFE_K floor
  // keeps it comfortably under 1 for any rect, and an extend can only ever
  // raise rx/ry further (safer), never lower them past that floor.
  const rx = Math.max(halfW / CORNER_SAFE_K, halfW + Math.min(90, w * 0.22));
  const ry = Math.max(halfH / CORNER_SAFE_K, halfH + Math.min(90, h * 0.22));
  const cornerFrac = Math.hypot(halfW / rx, halfH / ry);
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

/**
 * A second, stronger scrim confined to one rect (never the whole copy
 * block): a solid disc that exactly covers the rect's corners, then
 * feathers out over at least EXTRA_SCRIM_FEATHER px so there is no visible
 * edge. cornerFrac (the gradient's inner circle, in the scaled ellipse
 * space) MUST stay below 1 -- a radial gradient's inner stop past its
 * outer radius is undefined per-pixel, which silently let bright pixels
 * leak through a wide, short rect (paint.ts's own paragraph rect) even at
 * alpha 1. Deriving rx/ry from a fixed K keeps hypot(halfW/rx, halfH/ry)
 * safely under 1 for ANY aspect ratio (hypot(K, K) with K = 0.7 is ~0.99),
 * and the feather floor can only ever shrink that fraction further.
 */
function paintExtraScrim(ctx: CanvasRenderingContext2D, rect: Rect) {
  const pad = 6;
  const x = rect.x - pad;
  const y = rect.y - pad;
  const w = rect.width + pad * 2;
  const h = rect.height + pad * 2;
  const cx = x + w / 2;
  const cy = y + h / 2;
  const halfW = w / 2;
  const halfH = h / 2;
  const rx = Math.max(halfW / CORNER_SAFE_K, halfW + EXTRA_SCRIM_FEATHER);
  const ry = Math.max(halfH / CORNER_SAFE_K, halfH + EXTRA_SCRIM_FEATHER);
  const cornerFrac = Math.hypot(halfW / rx, halfH / ry);
  ctx.save();
  ctx.translate(cx, cy);
  ctx.scale(rx, ry);
  const grad = ctx.createRadialGradient(0, 0, cornerFrac, 0, 0, 1);
  grad.addColorStop(0, rgba(NAVY, EXTRA_SCRIM_ALPHA));
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

export function paint({
  ctx,
  w,
  h,
  graph,
  copyRect,
  paragraphRect,
  ctaRect,
}: PaintOptions) {
  ctx.clearRect(0, 0, w, h);

  // Edges first, under every node. Every edge is slate with a cyan tint --
  // near edges lean further into that tint (never toward white, which only
  // ever appears on a hub's own core dot) and get a touch more alpha and
  // width, which is what keeps the near band's rendered luminance clearly
  // above the "lines clearly visible" floor even after a thin, antialiased
  // stroke dilutes its own per-pixel colour below the nominal alpha
  // composite; far edges keep the lighter tint so depth fade still reads.
  ctx.lineCap = "round";
  for (const e of graph.edges) {
    ctx.lineWidth = e.lineWidth;
    const cyanRatio = e.near ? 0.7 : 0.4;
    ctx.strokeStyle = rgba(
      SLATE,
      e.alpha,
      { with: CYAN, ratio: cyanRatio },
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
  if (paragraphRect) {
    paintExtraScrim(ctx, paragraphRect);
  }
  if (ctaRect) {
    paintExtraScrim(ctx, ctaRect);
  }
  paintVignette(ctx, w, h);
}
